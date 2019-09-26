using Domain;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventStore
{
    struct SingleEvent
    {
        public string EventId;
        public long EventVersion;
        public string EventName;
        public string EventBody;
    }

    public class MemoryEventStore : IEventStore
    {
        private readonly List<SingleEvent> _events = new List<SingleEvent>();
        private readonly Dictionary<string, int> _latestEventVersion = new Dictionary<string, int>();

        public MemoryEventStore()
        {
            Initialize();
        }

        private void Initialize()
        {
            Console.WriteLine("Initializing Memory Event Store.");

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
        }

        public void SaveEvents(string aggregateType, string aggregateId, IEnumerable<Event> newEvents, int expectedVersion)
        {
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

                    SingleEvent singleEvent = new SingleEvent()
                    {
                        EventId = CreateId(aggregateType, aggregateId),
                        EventVersion = newAggregateVersion,
                        EventName = aggregateType,
                        EventBody = JsonConvert.SerializeObject(@event)
                    };

                    _events.Add(singleEvent);

                    if (_latestEventVersion.ContainsKey(singleEvent.EventId))
                        _latestEventVersion[singleEvent.EventId] = newAggregateVersion;
                    else
                        _latestEventVersion.Add(singleEvent.EventId, newAggregateVersion);

                    Console.WriteLine();
                    Console.WriteLine("***********************************************");
                    Console.WriteLine("*************** Event(s) Saved ****************");
                    Console.WriteLine("***********************************************");
                    Console.WriteLine(JsonConvert.SerializeObject(singleEvent, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving domain event {0}. Exception message: {1}", eventName, e.Message);
                Console.WriteLine("Events: {0}", JsonConvert.SerializeObject(newEvents));
                Console.WriteLine(e.StackTrace);
            }
        }

        private string CreateId(string aggregateType, string aggregateId)
        {
            return aggregateType + "_" + aggregateId;
        }

        public int GetAggregateVersion(string aggregateType, string aggregateId)
        {
            string id = CreateId(aggregateType, aggregateId);
            if (_latestEventVersion.ContainsKey(id))
                return _latestEventVersion[id];
            return -1;
        }

        private bool AggregateExists(string id)
        {
            foreach (var item in _events)
            {
                if (item.EventId.Equals(id))
                    return true;
            }
            return false;
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
            string id = CreateId(aggregateType, aggregateId);
            List<Event> events = new List<Event>();

            foreach (var item in _events)
            {
                events.Add(JsonConvert.DeserializeObject<Event>(item.EventBody));
            }

            events = events.OrderBy(o => o.Version).ToList();

            return events;
        }

        public bool Exists(string aggregateType, string aggregateId)
        {
            return AggregateExists(CreateId(aggregateType, aggregateId));
        }
    }
}
