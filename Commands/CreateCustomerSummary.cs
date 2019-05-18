using Newtonsoft.Json;
using Shared;

namespace Commands
{
    public class CreateCustomerSummary : Command
    {
        [JsonConstructor]
        protected CreateCustomerSummary()
        {
        }

        public CreateCustomerSummary(MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
