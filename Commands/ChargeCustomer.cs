using Newtonsoft.Json;
using Shared;

namespace Commands
{
    public class ChargeCustomer : Command
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public int ExpectedVersion { get; set; }

        [JsonConstructor]
        protected ChargeCustomer()
        {
        }

        public ChargeCustomer(decimal amount, int expectedVersion, MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
            Amount = amount;
            ExpectedVersion = expectedVersion;
        }
    }
}
