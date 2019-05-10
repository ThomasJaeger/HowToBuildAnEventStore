using Domain;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventStore
{
    struct Aggregates
    {
        public string AggregateId;
        public string AggregateType;
        public long AggregateVersion;
        public string ChangesetId;
    }

    struct Changesets
    {
        public string ChangesetId;
        public string AggregateId;
        public long AggregateVersion;
        public string ChangesetContent;
        public long NumberOfEvents;
        public string ParentChangesetId;
    }

    public class MemoryEventStore : IEventStore
    {
        private readonly Dictionary<string, Aggregates> _dbAggregates = new Dictionary<string, Aggregates>();
        private readonly Dictionary<string, Changesets> _dbChangesets = new Dictionary<string, Changesets>();

        private const string NULL_STRING = "Null";

        public MemoryEventStore()
        {
            Initialize();
        }

        private void Initialize()
        {
            Console.WriteLine("Initializing Memory Event Store.");

            // Note: clients can not access data from other regions
            // AWS recommends to create clients for each region
            //_db = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast1);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                //Formatting = Formatting.Indented,
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
                //Console.WriteLine($"Did not find a changeset with itemAggregate.ChangesetId: {itemAggregate.ChangesetId}");
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
                //Console.WriteLine("Added domain event: " + JsonConvert.SerializeObject(tmp[i]));
                events.Add(tmp[i]);
            }

            return events;
        }

        public bool Exists(string aggregateId)
        {
            return AggregateExists(aggregateId);
        }

        public int GetAggregateVersion(string aggregateId)
        {
            return (int)_dbAggregates[aggregateId].AggregateVersion;
        }

        private bool AggregateExists(string id)
        {
            return _dbAggregates.ContainsKey(id);
        }

        private string GetChangesetId(string aggregateId)
        {
            if (string.IsNullOrEmpty(aggregateId))
            {
                Console.WriteLine("aggregateId is null in GetChangesetId");
                //throw new NullAggregateIdException();
            }

            return _dbAggregates[aggregateId].ChangesetId;
        }

        private void SaveAggregate(ChangesetDocument changeSet, string aggregateType, IEnumerable<Event> newEvents)
        {
            Aggregates aggregates = new Aggregates()
            {
                AggregateId = changeSet.AggregateId,
                AggregateVersion = changeSet.AggregateVersion,
                ChangesetId = changeSet.ChangesetId,
                AggregateType = aggregateType
            };
            if (_dbAggregates.ContainsKey(changeSet.AggregateId))
                _dbAggregates[changeSet.AggregateId] = aggregates;
            else
                _dbAggregates.Add(changeSet.AggregateId, aggregates);

            Console.WriteLine();
            Console.WriteLine("***********************************************");
            Console.WriteLine("*************** Aggregate Saved ***************");
            Console.WriteLine("***********************************************");
            Console.WriteLine(JsonConvert.SerializeObject(aggregates, Formatting.Indented));
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
            ChangesetDocument result = new ChangesetDocument
            {
                AggregateId = _dbChangesets[changeSetId].AggregateId,
                AggregateVersion = _dbChangesets[changeSetId].AggregateVersion,
                ChangesetId = _dbChangesets[changeSetId].ChangesetId,
                ParentChangesetId = _dbChangesets[changeSetId].ParentChangesetId,
                NumberOfEvents = _dbChangesets[changeSetId].NumberOfEvents,
                Content = _dbChangesets[changeSetId].ChangesetContent
            };

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
            Changesets changesets = new Changesets()
            {
                AggregateId = changeSet.AggregateId,
                AggregateVersion = changeSet.AggregateVersion,
                ChangesetContent = changeSet.Content,
                ChangesetId = changeSet.ChangesetId,
                NumberOfEvents = changeSet.NumberOfEvents,
                ParentChangesetId = changeSet.ParentChangesetId
            };

            _dbChangesets.Add(changeSet.ChangesetId, changesets);

            Console.WriteLine();
            Console.WriteLine("***********************************************");
            Console.WriteLine("*************** Changeset Saved ***************");
            Console.WriteLine("***********************************************");
            Console.WriteLine(JsonConvert.SerializeObject(changeSet, Formatting.Indented));

            return true;
        }
    }
}
