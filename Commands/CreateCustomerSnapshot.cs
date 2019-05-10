using Newtonsoft.Json;
using Shared;

namespace Commands
{
    /// <summary>
    /// Creates a rolling snapshot of an aggregate. This command can be
    /// sent when needed or on a regular and automatic basis when monitoring the
    /// number of domain events inside an aggregate.
    /// </summary>
    public class CreateCustomerSnapshot : Command
    {
        /// <summary>
        /// The unique aggregate identifier in the event store.
        /// </summary>
        public string AggregateId { get; set; }

        [JsonConstructor]
        protected CreateCustomerSnapshot()
        {
        }

        public CreateCustomerSnapshot(MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
