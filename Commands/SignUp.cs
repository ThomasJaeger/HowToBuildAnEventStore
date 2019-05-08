using Newtonsoft.Json;
using Shared;

namespace Commands
{
    public class SignUp : Command
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }

        [JsonConstructor]
        protected SignUp()
        {
        }

        public SignUp(MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
