using Newtonsoft.Json;
using Shared;

namespace Commands
{
    public class ChargeCustomer : Command
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";

        [JsonConstructor]
        protected ChargeCustomer()
        {
        }

        public ChargeCustomer(decimal amount, MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
