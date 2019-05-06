using System;

namespace Shared
{
    public class MessageCreateOptions
    {
        public DateTime? Created { get; set; }

        public string MessageId { get; set; }

        public CustomerId CustomerId { get; set; }

        public MessageCreateOptions()
        {
        }

        public MessageCreateOptions(MessageCreateOptions mco)
        {
            if (mco == null)
                throw new Exception("MessageCreateOptions parameter must not be null when creating a new MessageCreateOptions.");

            // NOTE: MessageId is not assigned as it will be created either in a command or event class.
            Created = mco.Created;
            CustomerId = mco.CustomerId;
        }
    }
}
