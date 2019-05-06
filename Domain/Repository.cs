using Shared;
using System.Collections.Generic;

namespace Domain
{
    public class Repository<T> : IRepository<T> where T : AggregateRoot, new()
    {
        private readonly IEventStore _storage;

        public Repository(IEventStore storage)
        {
            _storage = storage;
        }

        public void Save(AggregateRoot aggregate, string aggregateId, int expectedVersion)
        {
            _storage.SaveEvents(aggregate.GetType().Name, aggregateId, aggregate.GetUncommittedChanges(),
                expectedVersion);
            aggregate.MarkChangesAsCommitted();
        }

        public T GetById(string id)
        {
            var obj = new T();
            try
            {
                List<Event> e = _storage.GetEventsForAggregate(id);
                obj.LoadsFromHistory(e);
            }
            catch (AggregateNotFoundException ex)
            {
                //Console.WriteLine(ex +" in Repository, id="+id);
            }
            return obj;
        }

        public List<Event> GetEventsForAggregate(string aggregateId)
        {
            return _storage.GetEventsForAggregate(aggregateId);
        }

        public bool Exists(string aggregateId)
        {
            return _storage.Exists(aggregateId);
        }

        public int GetAggregateVersion(string aggregateId)
        {
            return _storage.GetAggregateVersion(aggregateId);
        }
    }
}
