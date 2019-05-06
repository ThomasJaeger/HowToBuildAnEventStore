using Newtonsoft.Json;
using System;

namespace Shared
{
    public class Command : Message, ICommand
    {
        public DateTime Created { get; set; }
        public string MessageId { get; }
        public CustomerId CustomerId { get; set; }

        [JsonConstructor]
        protected Command()
        {
            MessageId = "cmd_" + Guid.NewGuid();
        }

        protected Command(MessageCreateOptions messageCreateOptions)
        {
            if (messageCreateOptions == null)
                throw new Exception("messageCreateOptions must not be null when creating Command and passing in messageCreateOptions.");

            if (messageCreateOptions.Created == null)
                throw new Exception("Created must not be null in Command message.");

            Created = messageCreateOptions.Created.Value;
            MessageId = "cmd_" + Guid.NewGuid();
            CustomerId = messageCreateOptions.CustomerId;
        }

        public MessageCreateOptions GetMessageCreateOptions()
        {
            return new MessageCreateOptions()
            {
                CustomerId = CustomerId,
                Created = Created,
                MessageId = MessageId,
            };
        }
    }
}
