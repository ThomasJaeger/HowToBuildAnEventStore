using System;

namespace Shared
{
    /// <summary>
    /// Every message has 3 ids, MessageId, CorrelationId, CausationId. When we are
    /// responding to a message (either a command or an event) we copy the
    /// CorrelationId of the message we are responding to to our message.
    /// 
    /// The CausationId of our message is the MessageId of the message we are
    /// responding to.
    ///
    /// https://www.infoq.com/news/2017/11/event-sourcing-microservices
    /// https://groups.google.com/forum/#!msg/dddcqrs/qGYC6qZEqOI/LhQup9v7EwAJ
    /// </summary>
    public interface Message
    {
        DateTime Created { get; set; }

        string MessageId { get; set; }
        string CorrelationId { get; set; }
        string CausationId { get; set; }

        TenantId TenantId { get; set; }
        UserId UserId { get; set; }
    }
}
