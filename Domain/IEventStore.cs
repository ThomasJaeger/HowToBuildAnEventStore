using Shared;
using System.Collections.Generic;

namespace Domain
{
    public interface IEventStore
    {
        void SaveEvents(string aggregateType, string aggregateId, IEnumerable<Event> newEvents, int expectedVersion);
        List<Event> GetEventsForAggregate(string aggregateType, string aggregateId);
        bool Exists(string aggregateType, string aggregateId);
        int GetAggregateVersion(string aggregateType, string aggregateId);
    }
}
