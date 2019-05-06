using Domain;
using EventStore;
using Newtonsoft.Json;

namespace FakeStuff
{
    public sealed class TheBackend
    {
        private static readonly TheBackend _instance = new TheBackend();

        private CommandHandlers _commandHandlers;

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static TheBackend()
        {
        }

        private TheBackend()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            IEventStore eventStore = new DynamoDBEventStore();

            _commandHandlers = new CommandHandlers(new Repository<Customer>(eventStore));
        }

        public static TheBackend Instance
        {
            get
            {
                return _instance;
            }
        }

        public static void ProcessCommand()
        {
            //string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            //_instance._queue.Enqueue(json);

            //Console.WriteLine();
            //Console.WriteLine("The Internet enqueued command:");
            //Console.WriteLine(json);
        }
    }
}
