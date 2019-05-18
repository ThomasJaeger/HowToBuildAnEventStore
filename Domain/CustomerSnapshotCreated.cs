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
        public string FirstName { get; set; }
        public string Lastname { get; set; }
        public string Password { get; set; }

        [JsonConstructor]
        protected CustomerSnapshotCreated()
        {
        }

        public CustomerSnapshotCreated(Money accountBalance, bool delinquent, string description, string email, DateTime created,
            string firstName, string lastName, string password, MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
            AccountBalance = accountBalance;
            Delinquent = delinquent;
            Description = description;
            Email = email;
            Created = created;
            FirstName = firstName;
            Lastname = lastName;
            Password = password;
            IsSnapshot = true;
        }
    }
}
