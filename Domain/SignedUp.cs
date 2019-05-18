using Newtonsoft.Json;
using Shared;

namespace Domain
{
    public class SignedUp : Event
    {
        public string FirstName { get; set; }
        public string Lastname { get; set; }
        public string Password { get; set; }

        [JsonConstructor]
        protected SignedUp()
        {
        }

        public SignedUp(string firstName, string lastName, string password, MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
            FirstName = firstName;
            Lastname = lastName;
            Password = password;
        }
    }
}
