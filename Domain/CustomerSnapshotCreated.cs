using Newtonsoft.Json;
using Shared;
using System;
using TeixeiraSoftware.Finance;

namespace Domain
{
    public class CustomerSnapshotCreated : Event
    {
        public Money AccountBalance { get; set; }
        public bool Delinquent { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }

        [JsonConstructor]
        protected CustomerSnapshotCreated()
        {
        }

        public CustomerSnapshotCreated(Money accountBalance, bool delinquent, string description, string email, DateTime created,
            MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
            AccountBalance = accountBalance;
            Delinquent = delinquent;
            Description = description;
            Email = email;
            Created = created;
            IsSnapshot = true;
        }
    }
}
