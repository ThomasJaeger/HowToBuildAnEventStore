using Newtonsoft.Json;
using Shared;
using TeixeiraSoftware.Finance;

namespace Domain
{
    public class CustomerCharged : Event
    {
        public Money Amount { get; set; }

        [JsonConstructor]
        protected CustomerCharged()
        {
        }

        public CustomerCharged(Money amount, MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
            Amount = amount;
        }
    }
}
