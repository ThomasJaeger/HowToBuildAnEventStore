using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Domain
{
    public abstract class AggregateRoot
    {
        private readonly List<Event> _changes = new List<Event>();

        public int Version { get; set; } = -1;

        public IEnumerable<Event> GetUncommittedChanges()
        {
            return _changes;
        }

        public void MarkChangesAsCommitted()
        {
            _changes.Clear();
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
