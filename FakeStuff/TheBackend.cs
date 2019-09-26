using System;
using Domain;
using EventStore;
using MemoryProblemEventPublisher;
using Newtonsoft.Json;
using ReadModel.Interfaces;
using ReadModel.MySql;
using ReadModel.Queries;
using Shared;

namespace FakeStuff
{
    public sealed class TheBackend
    {
        private static readonly TheBackend _instance = new TheBackend();

        private CommandHandlers _commandHandlers;
        private Repository<Customer> _customerRepository;

        private readonly IReadModelRepository _readModelRepository;

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

            //IEventStore eventStore = new DynamoDBEventStore();
            IEventStore eventStore = new MemoryEventStore();

            _customerRepository = new Repository<Customer>(eventStore);

            IProblemEventPublisher problemEventPublisher = new MemoryPublisher();

            _commandHandlers = new CommandHandlers(_customerRepository, problemEventPublisher);

            _readModelRepository = new MySqlRepository();
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
            string json = TheInternet.Dequeue();
            Command cmd = JsonConvert.DeserializeObject<Command>(json);
            _instance._commandHandlers.Handle(cmd);
        }

        public static int GetAggregateVersion(string aggregateId)
        {
            return _instance._customerRepository.GetAggregateVersion(Aggregates.Customer, aggregateId);
        }

        public static string QueryCustomerDetails(QueryCustomerDetails query)
        {
            return _instance._readModelRepository.Handle(query);
        }

        public static string QueryCustomerList(QueryCustomerList query)
        {
            return _instance._readModelRepository.Handle(query);
        }

        public static string DisplayAllDomainEvents(string aggregateId)
        {
            return JsonConvert.SerializeObject(_instance._customerRepository.GetEventsForAggregate(Aggregates.Customer, aggregateId), Formatting.Indented);
        }
    }
}
