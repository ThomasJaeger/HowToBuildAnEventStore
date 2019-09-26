using Shared;
using System.Collections.Generic;

namespace Domain
{
    public interface IRepository<T> where T : IAggregateRoot, new()
    {
        void Save(IAggregateRoot aggregate, string aggregateId, int expectedVersion);
        T GetById(string aggregateType, string aggregateId);
        List<Event> GetEventsForAggregate(string aggregateType, string aggregateId);
        bool Exists(string aggregateType, string aggregateId);
        int GetAggregateVersion(string aggregateType, string aggregateId);
    }
}
