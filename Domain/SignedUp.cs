using Newtonsoft.Json;
using Shared;

namespace Domain
{
    public class SignedUp : Event
    {
        [JsonConstructor]
        protected SignedUp()
        {
        }

        public SignedUp(MessageCreateOptions messageCreateOptions) : base(messageCreateOptions)
        {
        }
    }
}
