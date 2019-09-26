using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Domain
{
    public abstract class AggregateRoot : IAggregateRoot
    {
        public TenantId TenantId { get; set; }
        private readonly List<Event> _changes = new List<Event>();

        // Contains any business rules that were violated while processing a command
        // These broken rules are not persisted in the event store but used by the
        // command handler(s) to further deal with them.
        private List<BrokenRule> _brokenRules = new List<BrokenRule>();

        public int Version { get; set; } = -1;

        public List<BrokenRule> GetBrokenRules()
        {
            return _brokenRules;
        }

        public void AddBrokenRule(BrokenRule brokenRule)
        {
            if (!_brokenRules.Contains(brokenRule))
            {
                _brokenRules.Add(brokenRule);
                Console.WriteLine($"Added broken rule #{brokenRule.Type.Value}, {brokenRule.Type.DisplayName}, {brokenRule.Type.Description}");
            }
        }

        /// <summary>
        /// Child aggregates can further handle the clearing of the rules
        /// before they are cleared by the parent.
        /// </summary>
        protected void ResolveBrokenRules()
        {
            _brokenRules.Clear();
        }

        public IEnumerable<Event> GetUncommittedChanges()
        {
            return _changes;
        }

        public void MarkChangesAsCommitted()
        {
            _changes.Clear();
            ResolveBrokenRules();
        }

        public void LoadsFromHistory(IList<Event> history)
        {
            for (int i = 0; i < history.Count; i++)
            {
                ApplyChange(history[i], false);
            }
        }

        protected void ApplyChange(Event @event)
        {
            TenantId = @event.TenantId;
            ApplyChange(@event, true);
        }

        // push atomic aggregate changes to local history for further processing (MemoryEventStore.SaveEvents)
        private void ApplyChange(Event @event, bool isNew)
        {
            Type[] types = { @event.GetType() };
            MethodInfo dynMethod = this.GetType().GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            dynMethod.Invoke(this, new object[] { @event });
            if (isNew) _changes.Add(@event);
            Version++;
        }
    }
}
