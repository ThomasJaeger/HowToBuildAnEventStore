using Domain;
using Newtonsoft.Json;
using System;

namespace MemoryProblemEventPublisher
{
    public class MemoryPublisher : IProblemEventPublisher
    {
        public void Publish(ProblemOccured e)
        {
            string json = JsonConvert.SerializeObject(e);

            Console.WriteLine();
            Console.WriteLine("***********************************************");
            Console.WriteLine("*********** Problem event published ***********");
            Console.WriteLine("***********************************************");
            Console.WriteLine(json);
        }
    }
}
