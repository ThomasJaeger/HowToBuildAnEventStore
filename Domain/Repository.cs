using System;
using System.Collections.Generic;
using Shared;

namespace Domain
{
    public class Repository<T> : IRepository<T> where T : IAggregateRoot, new()
    {
        private readonly IEventStore _storage;

        public Repository(IEventStore storage)
        {
            _storage = storage;
        }

        public void Save(IAggregateRoot aggregate, string aggregateId, int expectedVersion)
        {
            //Common.ValidateIdWithException(aggregateId);
            _storage.SaveEvents(aggregate.GetType().Name, aggregateId, aggregate.GetUncommittedChanges(),
                expectedVersion);
            aggregate.MarkChangesAsCommitted();
        }

        public T GetById(string aggregateType, string aggregateId)
        {
            var obj = new T();
            try
            {
                List<Event> e = _storage.GetEventsForAggregate(aggregateType, aggregateId);
                //Console.WriteLine("GetById event list: " + JsonConvert.SerializeObject(e));
                obj.LoadsFromHistory(e);
            }
            catch (AggregateNotFoundException ex)
            {
                Console.WriteLine("*** ERROR ***: Aggregate not found in event store. Aggregate Id: " + aggregateId);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** ERROR ***: GetById Function in event store. Aggregate Id: " + aggregateId);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return obj;
        }

        public List<Event> GetEventsForAggregate(string aggregateType, string aggregateId)
        {
            return _storage.GetEventsForAggregate(aggregateType, aggregateId);
        }

        public bool Exists(string aggregateType, string aggregateId)
        {
            return _storage.Exists(aggregateType, aggregateId);
        }

        public int GetAggregateVersion(string aggregateType, string aggregateId)
        {
            return _storage.GetAggregateVersion(aggregateType, aggregateId);
        }
    }
}
