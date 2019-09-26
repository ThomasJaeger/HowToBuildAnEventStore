using Shared;
using System.Collections.Generic;

namespace Domain
{
    public interface IAggregateRoot
    {
        TenantId TenantId { get; set; }
        int Version { get; set; }

        void AddBrokenRule(BrokenRule brokenRule);
        List<BrokenRule> GetBrokenRules();
        IEnumerable<Event> GetUncommittedChanges();
        void LoadsFromHistory(IList<Event> history);
        void MarkChangesAsCommitted();
    }
}
