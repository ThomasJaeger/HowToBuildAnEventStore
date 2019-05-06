using Newtonsoft.Json;
using System;

namespace Shared
{
    public class Event : Message
    {
        public DateTime Created { get; set; }
        public string MessageId { get; }
        public CustomerId CustomerId { get; set; }

        public int Version { get; set; }
        public string DomainEvent { get; set; }
        public bool IsSnapshot { get; set; }
        public ProblemCode ProblemCode { get; set; } = ProblemCode.None;

        [JsonConstructor]
        protected Event()
        {
            MessageId = "evt_" + Guid.NewGuid();
        }

        protected Event(MessageCreateOptions messageCreateOptions)
        {
            if (messageCreateOptions == null)
                throw new Exception("messageCreateOptions must not be null when creating Event and passing in messageCreateOptions.");

            if (messageCreateOptions.Created == null)
                throw new Exception("Created must not be null in Event message.");

            Version = 1;
            Created = messageCreateOptions.Created.Value;
            MessageId = "evt_" + Guid.NewGuid();
            CustomerId = messageCreateOptions.CustomerId;
            DomainEvent = GetType().Name;
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
