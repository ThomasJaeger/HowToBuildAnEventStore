using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FakeStuff
{
    public sealed class TheInternet
    {
        private static readonly TheInternet _instance = new TheInternet();

        private Queue<string> _queue;

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static TheInternet()
        {
        }

        private TheInternet()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            _queue = new Queue<string>();
        }

        public static TheInternet Instance
        {
            get
            {
                return _instance;
            }
        }

        public static void Enqueue(object item)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            _instance._queue.Enqueue(json);

            Console.WriteLine();
            Console.WriteLine("The Internet enqueued command:");
            Console.WriteLine(json);
        }

        public static object Dequeue()
        {
            if (_instance._queue.Count > 0)
                return _instance._queue.Dequeue();
            return "";
        }
    }
}
