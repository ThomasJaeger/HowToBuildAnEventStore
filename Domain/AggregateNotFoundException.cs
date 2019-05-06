using System;

namespace Domain
{
    public class AggregateNotFoundException : Exception
    {
        public AggregateNotFoundException()
        {
            Console.WriteLine("AggregateNotFoundException");
        }

        public AggregateNotFoundException(string message)
            : base(message)
        {
            Console.WriteLine(message);
        }

        public AggregateNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
            Console.WriteLine(message);
            Console.WriteLine(innerException.ToString());
        }
    }
}
