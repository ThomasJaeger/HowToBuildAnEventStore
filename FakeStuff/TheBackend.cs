﻿using Domain;
using EventStore;
using MemoryProblemEventPublisher;
using Newtonsoft.Json;
using Shared;

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
            IProblemEventPublisher problemEventPublisher = new MemoryPublisher();

            _commandHandlers = new CommandHandlers(new Repository<Customer>(eventStore), problemEventPublisher);
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
    }
}