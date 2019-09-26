using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;

namespace EventStore
{
    public class DynamoDBEventStore : IEventStore
    {
        private const string PK = "PK";
        private const string SK = "SK";
        private const string EVENT_NAME = "EventName";
        private const string SNAPSHOT = "Snapshot";

        private AmazonDynamoDBClient _db;
        private readonly string _table;

        public DynamoDBEventStore()
        {
            _table = Environment.GetEnvironmentVariable("ENV") + "-" + Environment.GetEnvironmentVariable("AWS_REGION") + "-EventStore";
            Initialize();
        }

        private void Initialize()
        {
            Console.WriteLine("Initializing DynamoDB Event Store.");
            _db = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast1);
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
        }

        public void SaveEvents(string aggregateType, string aggregateId, IEnumerable<Event> newEvents, int expectedVersion)
        {
            // DynamoDB schema design
            //
            // PK               SK (aggregate version)  Snapshot (GSI)  Event attributes depending on domain event class
            // ---------------------------------------------------------------------------------------------------------
            // ClassName_Guid   0                   
            // ClassName_Guid   1
            // ClassName_Guid   2
            // ClassName_Guid   3                       3

            int currentAggregateVersion = GetAggregateVersion(aggregateType, aggregateId);

            if (currentAggregateVersion != expectedVersion && expectedVersion != -1)
            {
                throw new ConcurrencyException();
            }

            int newAggregateVersion = expectedVersion;
            string eventName = "";  // using a seperate variable in case we have exceptions and we can log the event name

            try
            {
                foreach (var @event in newEvents)
                {
                    eventName = @event.DomainEvent;
                    newAggregateVersion++;
                    var request = CreatePutItemRequest(@event, aggregateType, aggregateId, newAggregateVersion);

                    if (request != null)
                    {
                        request = AddBaseDomainEventAttributes(request, @event);
                        PutItemResponse response = _db.PutItemAsync(request).Result;
                    }
                    else
                    {
                        Console.WriteLine("Unable to save domain event: " + JsonConvert.SerializeObject(@event));
                    }
                }
            }
            catch (ConditionalCheckFailedException e)
            {
                // Already exists, do not allow to insert it again.
                Console.WriteLine("Trying to insert a dublicate item with primary key: " + aggregateType + "_" + aggregateId);
            }
            catch (Exception e)
            {
                Console.WriteLine("*****************************************************");
                Console.WriteLine("Error saving domain event {0}.", eventName);
                Console.WriteLine("Exception message: {0}", e.Message);
                Console.WriteLine("Events: {0}", JsonConvert.SerializeObject(newEvents));
                Console.WriteLine(e.StackTrace);
            }
        }

        public PutItemRequest CreatePutItemRequest(Event @event, string aggregateType, string aggregateId, int newAggregateVersion)
        {
            string json = JsonConvert.SerializeObject(@event);
            string eventName = @event.GetType().Name;

            PutItemRequest request = new PutItemRequest
                {
                    TableName = _table,
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        {PK, new AttributeValue {S = aggregateType + "_" + aggregateId}},
                        {SK, new AttributeValue {N = newAggregateVersion.ToString()}},
                        {EVENT_NAME, new AttributeValue {S = eventName}},
                        {"EventBody", new AttributeValue {S = json}}
                    },
                    ConditionExpression = "attribute_not_exists(" + PK + ")"
                };
            return request;
        }

        private PutItemRequest AddBaseDomainEventAttributes(PutItemRequest request, Event e)
        {
            request.Item.Add("Created", new AttributeValue { S = e.Created.ToString() });
            request.Item.Add("MessageId", new AttributeValue { S = e.MessageId });
            request.Item.Add("CorrelationId", new AttributeValue { S = e.CorrelationId });
            request.Item.Add("CausationId", new AttributeValue { S = e.CausationId });
            request.Item.Add("TenantId", new AttributeValue { S = e.TenantId.Id });
            request.Item.Add("UserId", new AttributeValue { S = e.UserId.Id });

            return request;
        }

        public List<Event> GetEventsForAggregate(string aggregateType, string aggregateId)
        {
            // ******************************************************************************
            // Check and see if there is any snapshot created for the aggregate.
            // If there is a snapshot, read it in and the read the rest of the domain events.
            // ******************************************************************************
            //if (SnapshotAvailable())
            //{
            //    ReadDomainEventsWithSnapshot();
            //}
            //else
            //{
            return ReadDomainEvents(aggregateType, aggregateId);
            //}
        }

        private List<Event> ReadDomainEvents(string aggregateType, string aggregateId)
        {
            List<Event> events = new List<Event>();
            Dictionary<string, AttributeValue> lastKeyEvaluated = null;
            do
            {
                var request = new QueryRequest
                {
                    TableName = _table,
                    KeyConditionExpression = PK + " = :v_" + PK + " and " + SK + " >= :v_" + SK,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":v_" + PK, new AttributeValue {S = aggregateType + "_" + aggregateId}},
                        {":v_" + SK, new AttributeValue {N = "0"}},
                    },
                    ConsistentRead = true,   // <<<======= Make sure at least one shard confirms latest PUT committed in the DynamoDB backend
                    ExclusiveStartKey = lastKeyEvaluated
                };

                var response = _db.QueryAsync(request).Result;
                foreach (Dictionary<string, AttributeValue> item in response.Items)
                {
                    events.Add(DeserializeDomainEvent(item));
                }

                lastKeyEvaluated = response.LastEvaluatedKey;

            } while (lastKeyEvaluated != null && lastKeyEvaluated.Count != 0);

            return events;
        }

        private Event DeserializeDomainEvent(Dictionary<string, AttributeValue> a)
        {
            Event e = JsonConvert.DeserializeObject<Event>(a["EventBody"].S);

            e.Version = Convert.ToInt32(a["SK"].N);
            e.DomainEvent = a["EventName"].S;

            return e;
        }

        public bool Exists(string aggregateType, string aggregateId)
        {
            return AggregateExists(aggregateType, aggregateId);
        }

        public int GetAggregateVersion(string aggregateType, string aggregateId)
        {
            try
            {
                var request = new QueryRequest
                {
                    TableName = _table,
                    KeyConditionExpression = PK + " = :v_" + PK + " and " + SK + " >= :v_" + SK,
                    ConsistentRead = true,   // <<<======= Make sure at least one shard confirms latest PUT committed in the DynamoDB backend
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":v_" + PK, new AttributeValue {S = aggregateType + "_" + aggregateId}},
                        {":v_" + SK, new AttributeValue {N = "0"}},
                    },
                    ScanIndexForward = false  // https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html
                };
                var response = _db.QueryAsync(request).Result;
                foreach (var item in response.Items)
                {
                    if (item.Count > 0)
                    {
                        return Convert.ToInt32(item[SK].N);
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        private bool AggregateExists(string aggregateType, string aggregateId)
        {
            // https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/LowLevelDotNetQuerying.html
            try
            {
                var request = new QueryRequest
                {
                    TableName = _table,
                    KeyConditionExpression = PK + " = :v_" + PK + " and " + SK + " = :v_" + SK,
                    ConsistentRead = true,   // <<<======= Make sure at least one shard confirms latest PUT committed in the DynamoDB backend
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":v_" + PK, new AttributeValue {S = aggregateType + "_" + aggregateId}},
                        {":v_" + SK, new AttributeValue {N = "0"}},
                    }
                };

                var response = _db.QueryAsync(request).Result;
                //var response = Task.Run(() => _db.QueryAsync(request)).Result;  // https://stackoverflow.com/questions/17248680/await-works-but-calling-task-result-hangs-deadlocks
                // var response = await _db.QueryAsync(request).ConfigureAwait(false);
                return response.Items.Count > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }
    }
}
