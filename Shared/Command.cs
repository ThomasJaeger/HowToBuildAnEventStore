using Newtonsoft.Json;
using System;

namespace Shared
{
    public class Command : Message, ICommand
    {
        public DateTime Created { get; set; }
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
        public TenantId TenantId { get; set; }
        public UserId UserId { get; set; }
        public CustomerId CustomerId { get; set; }

        [JsonConstructor]
        protected Command()
        {
            //MessageId = "cmd_" + Guid.NewGuid();
        }

        protected Command(MessageCreateOptions messageCreateOptions)
        {
            if (messageCreateOptions == null)
                throw new Exception("messageCreateOptions must not be null when creating Command and passing in messageCreateOptions.");

            if (messageCreateOptions.Created == null)
                throw new Exception("Created must not be null in Command message.");

            Created = messageCreateOptions.Created.Value;

            if (string.IsNullOrEmpty(messageCreateOptions.MessageId))
                MessageId = "cmd_" + Guid.NewGuid();
            else
                MessageId = messageCreateOptions.MessageId;

            if (string.IsNullOrEmpty(messageCreateOptions.CorrelationId))
                CorrelationId = MessageId;
            else
                CorrelationId = messageCreateOptions.CorrelationId;

            if (string.IsNullOrEmpty(messageCreateOptions.CausationId))
                CausationId = MessageId;
            else
                CausationId = messageCreateOptions.CausationId;

            TenantId = messageCreateOptions.TenantId;
            CustomerId = messageCreateOptions.CustomerId;
            UserId = messageCreateOptions.UserId;
        }

        public MessageCreateOptions GetMessageCreateOptions()
        {
            return new MessageCreateOptions()
            {
                TenantId = TenantId,
                UserId = UserId,
                CustomerId = CustomerId,
                Created = Created,
                CausationId = CausationId,
                MessageId = MessageId,
                CorrelationId = CorrelationId,
            };
        }
    }
}
