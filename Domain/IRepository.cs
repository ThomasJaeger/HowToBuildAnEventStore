using Shared;
using System.Collections.Generic;

namespace Domain
{
    public interface IRepository<T> where T : AggregateRoot, new()
    {
        void Save(AggregateRoot aggregate, string aggregateId, int expectedVersion);
        T GetById(string aggregateId);
        List<Event> GetEventsForAggregate(string aggregateId);
        bool Exists(string aggregateId);
        int GetAggregateVersion(string aggregateId);
    }
}
