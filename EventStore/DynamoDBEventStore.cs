using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventStore
{
    public class DynamoDBEventStore : IEventStore
    {
        private AmazonDynamoDBClient _db;
        private const string NULL_STRING = "Null";
        private readonly string _tableAggregates;
        private readonly string _tableChangeSets;
        private const string AGGREGATE_ID = "AggregateId";
        private const string AGGREGATE_VERSION = "AggregateVersion";
        private const string CHANGESET_ID = "ChangeSetId";
        private const string AGGREGATE_TYPE = "AggregateType";
        private const string PARENT_CHANGESET_ID = "ParentChangesetId";
        private const string NUMBER_OF_EVENTS = "NumberOfEvents";
        private const string CHANGESET_CONTENT = "ChangesetContent";

        public DynamoDBEventStore()
        {
            _tableAggregates = "Aggregates";
            _tableChangeSets = "ChangeSets";
            Initialize();
        }

        private void Initialize()
        {
            Console.WriteLine("Initializing DynamoDB Event Store.");

            // Note: clients can not access data from other regions
            // AWS recommends to create clients for each region
            _db = new AmazonDynamoDBClient(Amazon.RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")));

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
        }

        public void SaveEvents(string aggregateType, string aggregateId, IEnumerable<Event> newEvents, int expectedVersion)
        {
            ChangesetDocument changeSet = null;

            try
            {
                // First, check if the aggregagte has ever been stored before (the aggregagte's id rather)
                if (AggregateExists(aggregateId))
                {
                    // Get the latest changeset id
                    string changesetId = GetChangesetId(aggregateId);

                    // For performance reasons, we only need to load the head changeset first
                    // to check for concurrency violation.
                    ChangesetDocument headChangeSet = GetChangeSet(changesetId);

                    // If a concurrency violation exists, we abort the rest of the operation.
                    // The client should catch the exception and retry the operation again.
                    CheckForConcurrencyViolation(headChangeSet, expectedVersion);

                    // All good, commit all new domain events into a new changeset.
                    // When new changeset was persisted successfully, update the
                    // aggregate with the new changeset head id.
                    changeSet = CreateNewChangeSet(aggregateId, newEvents, expectedVersion, aggregateType, headChangeSet);
                }
                else
                {
                    // Persist domain events for the first time. This changeset
                    // won't have a parent.
                    //Console.WriteLine($"aggregateId '{aggregateId}' does not exist. Trying to create a new changeset.");
                    changeSet = CreateNewChangeSet(aggregateId, newEvents, expectedVersion, aggregateType);
                }

                if (SaveChangeSet(changeSet))
                    // Just stores a rerefence to the latest changeset with the aggregagte's id
                    // It does not store the actual aggregagte.
                    SaveAggregate(changeSet, aggregateType, newEvents);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public List<Event> GetEventsForAggregate(string aggregateId)
        {
            List<Event> events = new List<Event>();
            List<Event> tmp = new List<Event>();
            List<Event> changeSetEvents = new List<Event>();

            // 1. Read head changeset
            // 2. If head changeset has a parent, read next changeset until no more parent changeset

            // Get the latest changeset id
            string changesetId = GetChangesetId(aggregateId);
            if (string.IsNullOrEmpty(changesetId))
            {
                return events; // Did not find a DynamoDbItemAggregate
            }

            // Get all events for the first changeset (the head changeset)
            ChangesetDocument changeSet = GetChangeSet(changesetId);
            if (changeSet == null)
            {
                return events;
            }

            changeSetEvents = JsonConvert.DeserializeObject<List<Event>>(changeSet.Content);

            // Force adding events in correct order (don't use .AddRange)
            // Foreach loop is slower since it uses an indexer
            for (int i = 0; i < changeSetEvents.Count; i++)
                tmp.Add(changeSetEvents[i]);

            while (!changeSet.ParentChangesetId.Equals(NULL_STRING))
            {
                changeSet = GetChangeSet(changeSet.ParentChangesetId);
                changeSetEvents = JsonConvert.DeserializeObject<List<Event>>(changeSet.Content);

                // Foreach loop is slower since it uses an indexer
                for (int i = 0; i < changeSetEvents.Count; i++)
                    tmp.Add(changeSetEvents[i]);

                // The line below will stop reading domain events until
                // it finds the first snapshot from the top of the stack.
                if (changeSet.IsSnapshot) break;
            }

            // Put events in correct order
            for (int i = tmp.Count - 1; i >= 0; i--)
            {
                events.Add(tmp[i]);
            }

            return events;
        }

        public bool Exists(string aggregateId)
        {
            return AggregateExists(aggregateId);
        }

        private bool AggregateExists(string id)
        {
            // https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/LowLevelDotNetQuerying.html
            try
            {
                var request = new QueryRequest
                {
                    TableName = _tableAggregates,
                    KeyConditionExpression = AGGREGATE_ID + " = :v_" + AGGREGATE_ID,
                    ConsistentRead = true,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":v_" + AGGREGATE_ID, new AttributeValue {S = id}}
                    }
                };

                var response = _db.QueryAsync(request).Result;
                return response.Items.Count > 0;
            }
            catch (AmazonDynamoDBException e)
            {
                Console.WriteLine("Error locating aggregagte id '" + id + "' in DynamoDB table '" + _tableAggregates +
                                  "'");
                Console.WriteLine("Amazon error code: {0}", string.IsNullOrEmpty(e.ErrorCode) ? "None" : e.ErrorCode);
                Console.WriteLine("Exception message: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        private string GetChangesetId(string aggregateId)
        {
            if (string.IsNullOrEmpty(aggregateId))
            {
                Console.WriteLine("aggregateId is null in GetChangesetId");
                //throw new NullAggregateIdException();
            }

            string result = "";

            try
            {
                var request = new GetItemRequest
                {
                    TableName = _tableAggregates,
                    Key = new Dictionary<string, AttributeValue>() { { AGGREGATE_ID, new AttributeValue { S = aggregateId } } },
                    ConsistentRead = true   // Important to enforce consistent reads in this case
                };
                var response = _db.GetItemAsync(request).Result;
                var attributeMap = response.Item; // Attribute list in the response.
                if (attributeMap.Count > 0) // If item does not exist, attributeMap.Count will be 0
                {
                    result = attributeMap[CHANGESET_ID].S;
                }
                return result;
            }
            catch (AmazonDynamoDBException e)
            {
                Console.WriteLine("Error getting item for aggregate '" + aggregateId + "' in DynamoDB table '" + _tableAggregates + "''");
                Console.WriteLine("Amazon error code: {0}", string.IsNullOrEmpty(e.ErrorCode) ? "None" : e.ErrorCode);
                Console.WriteLine("Exception message: {0}", e.Message);
            }
            return result;
        }

        private void SaveAggregate(ChangesetDocument changeSet, string aggregateType, IEnumerable<Event> newEvents)
        {
            try
            {
                var request = new PutItemRequest
                {
                    TableName = _tableAggregates,
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        { AGGREGATE_ID, new AttributeValue { S = changeSet.AggregateId }},
                        { AGGREGATE_VERSION, new AttributeValue { N = changeSet.AggregateVersion.ToString() }},
                        { CHANGESET_ID, new AttributeValue { S = changeSet.ChangesetId }},
                        { AGGREGATE_TYPE, new AttributeValue { S = aggregateType }}
                    }
                };
                PutItemResponse response = _db.PutItemAsync(request).Result;
            }
            catch (ConditionalCheckFailedException e)
            {
                // Already exists, do not allow to insert it again.
                Console.WriteLine("Trying to insert a dublicate item with " + AGGREGATE_ID + ": " + changeSet.AggregateId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving aggregagte '" + changeSet.AggregateId + "' in DynamoDB table '" + _tableAggregates + "' with changeset id '" +
                              changeSet.ChangesetId + "'");
                Console.WriteLine("Exception message: {0}", e.Message);
            }
        }

        private void CheckForConcurrencyViolation(ChangesetDocument headChangeSet, int expectedVersion)
        {
            // check whether latest event version matches current aggregate version
            // otherwise -> throw exception
            if (headChangeSet != null)
                if ((headChangeSet.AggregateVersion != expectedVersion) && (expectedVersion != -1))
                    throw new ConcurrencyException();
        }

        private ChangesetDocument GetChangeSet(string changeSetId)
        {
            ChangesetDocument result = null;
            try
            {
                var request = new GetItemRequest
                {
                    TableName = _tableChangeSets,
                    Key = new Dictionary<string, AttributeValue>() { { CHANGESET_ID, new AttributeValue { S = changeSetId } } },
                    ConsistentRead = true   // Important to enforce consistent reads in this case
                };
                var response = _db.GetItemAsync(request).Result;
                var attributeMap = response.Item; // Attribute list in the response.
                if (attributeMap.Count > 0) // If item does not exist, attributeMap.Count will be 0
                {
                    result = new ChangesetDocument
                    {
                        AggregateId = attributeMap[AGGREGATE_ID].S,
                        AggregateVersion = Convert.ToInt32(attributeMap[AGGREGATE_VERSION].N),
                        ChangesetId = attributeMap[CHANGESET_ID].S,
                        ParentChangesetId = attributeMap[PARENT_CHANGESET_ID].S,
                        NumberOfEvents = Convert.ToInt32(attributeMap[NUMBER_OF_EVENTS].N),
                        Content = attributeMap[CHANGESET_CONTENT].S
                    };
                }
                return result;
            }
            catch (AmazonDynamoDBException e)
            {
                Console.WriteLine("Error getting item for changeset '" + changeSetId + "' in DynamoDB table '" + _tableChangeSets + "''");
                Console.WriteLine("Amazon error code: {0}", string.IsNullOrEmpty(e.ErrorCode) ? "None" : e.ErrorCode);
                Console.WriteLine("Exception message: {0}", e.Message);
            }
            return result;
        }

        private ChangesetDocument CreateNewChangeSet(string aggregateId, IEnumerable<Event> newEvents, int expectedVersion, string aggregateType, ChangesetDocument headChangeSet = null)
        {
            ChangesetDocument changeset = null;

            var domainEventVersion = expectedVersion;
            bool isSnapshot = false;

            foreach (var @event in newEvents)
            {
                domainEventVersion++;
                @event.Version = domainEventVersion;
                if (@event.IsSnapshot)
                    isSnapshot = true;
            }

            changeset = new ChangesetDocument
            {
                Content = JsonConvert.SerializeObject(newEvents),
                AggregateId = aggregateId,
                AggregateVersion = domainEventVersion,
                ChangesetId = aggregateId + ":" + domainEventVersion,
                NumberOfEvents = newEvents.Count(),
                ParentChangesetId = NULL_STRING,
                IsSnapshot = isSnapshot
            };

            if (headChangeSet != null)
                changeset.ParentChangesetId = headChangeSet.ChangesetId;

            return changeset;
        }

        private bool SaveChangeSet(ChangesetDocument changeSet)
        {
            try
            {
                var request = new PutItemRequest
                {
                    TableName = _tableChangeSets,
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        {CHANGESET_ID, new AttributeValue {S = changeSet.ChangesetId}},
                        {AGGREGATE_ID, new AttributeValue {S = changeSet.AggregateId}},
                        {AGGREGATE_VERSION, new AttributeValue {N = changeSet.AggregateVersion.ToString()}},
                        {PARENT_CHANGESET_ID, new AttributeValue {S = changeSet.ParentChangesetId}},
                        {NUMBER_OF_EVENTS, new AttributeValue {N = changeSet.NumberOfEvents.ToString()}},
                        {CHANGESET_CONTENT, new AttributeValue {S = changeSet.Content}}
                    },
                    ConditionExpression = "attribute_not_exists(" + CHANGESET_ID + ")"
                };
                PutItemResponse response = _db.PutItemAsync(request).Result;
                return true;
            }
            catch (ConditionalCheckFailedException e)
            {
                // Already exists, do not allow to insert it again.
                Console.WriteLine("Trying to insert a dublicate item with " + CHANGESET_ID + ": " + changeSet.ChangesetId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving changeset '" + changeSet.AggregateId + "' in DynamoDB table '" + _tableChangeSets + "' with changeset id '" +
                              changeSet.ChangesetId + "'");
                Console.WriteLine("Exception message: {0}", e.Message);
                Console.WriteLine("ChangesetId: {0}, AggregateId: {1}, AggregateVersion: {2}, ParentChangesetId: {3}, NumberOfEvents: {4}, Content: {5}",
                    changeSet.ChangesetId, changeSet.AggregateId, changeSet.AggregateVersion, changeSet.ParentChangesetId, changeSet.NumberOfEvents,
                    changeSet.Content);
            }
            return false;
        }

        public int GetAggregateVersion(string aggregateId)
        {
            int result = 0;
            try
            {
                var request = new GetItemRequest
                {
                    TableName = _tableAggregates,
                    Key = new Dictionary<string, AttributeValue>() { { AGGREGATE_ID, new AttributeValue { S = aggregateId } } },
                    ConsistentRead = true   // Important to enforce consistent reads in this case
                };
                var response = _db.GetItemAsync(request).Result;
                var attributeMap = response.Item; // Attribute list in the response.
                if (attributeMap.Count > 0) // If item does not exist, attributeMap.Count will be 0
                {
                    result = Convert.ToInt32(attributeMap[AGGREGATE_VERSION].N);
                }
                return result;
            }
            catch (AmazonDynamoDBException e)
            {
                Console.WriteLine("Error getting item for aggregate '" + aggregateId + "' in DynamoDB table '" + _tableAggregates + "''");
                Console.WriteLine("Amazon error code: {0}", string.IsNullOrEmpty(e.ErrorCode) ? "None" : e.ErrorCode);
                Console.WriteLine("Exception message: {0}", e.Message);
            }

            return result;
        }
    }
}
