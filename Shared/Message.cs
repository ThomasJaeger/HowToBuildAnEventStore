using System;

namespace Shared
{
    public interface Message
    {
        DateTime Created { get; set; }

        string MessageId { get; }

        CustomerId CustomerId { get; set; }
    }
}
