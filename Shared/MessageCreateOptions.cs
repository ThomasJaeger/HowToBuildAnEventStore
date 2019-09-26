using System;

namespace Shared
{
    public class MessageCreateOptions
    {
        public DateTime? Created { get; set; }

        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
        public TenantId TenantId { get; set; }
        public UserId UserId { get; set; }
        public CustomerId CustomerId { get; set; }

        public MessageCreateOptions()
        {
        }

        /// <summary>
        /// When supplying an existing MessageCreateOptions, it assign original MessageId
        /// as the CausationId for the new MessageCreateOptions to chain them. CorrelationId
        /// is copied forward into the new MessageCreateOptions.
        /// MessageId is not assigned as it will be created either in a command or event class.
        /// </summary>
        /// <param name="mco"></param>
        public MessageCreateOptions(MessageCreateOptions mco)
        {
            if (mco == null)
                throw new Exception("MessageCreateOptions parameter must not be null when creating a new MessageCreateOptions.");

            // NOTE: MessageId is not assigned as it will be created either in a command or event class.
            Created = mco.Created;
            CorrelationId = mco.CorrelationId;
            CausationId = mco.MessageId;
            TenantId = mco.TenantId;
            UserId = mco.UserId;
            CustomerId = mco.CustomerId;
        }
    }
}
