using Newtonsoft.Json;
using Shared;

namespace Domain
{
    public class ProblemOccured : Event
    {
        public string AggregateId;
        public string AggregateType;
        public int AggregateVersionInEventStore;
        public int AggregateVersionExpectedByClient;
        public string CommandIdThatTriggeredProblem;
        public string CommandThatTriggeredProblem;

        [JsonConstructor]
        protected ProblemOccured()
        {
        }

        public ProblemOccured(MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
