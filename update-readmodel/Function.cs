using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json;
using ReadModel.Interfaces;
using ReadModel.MySql;
using Shared;
using System;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace update_readmodel
{
    public class Function
    {
        private readonly IReadModelRepository _repository;

        public Function()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            _repository = new MySqlRepository();
        }

        public void FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            try
            {
                Console.WriteLine(JsonConvert.SerializeObject(snsEvent));
                foreach (var snsEventRecord in snsEvent.Records)
                {
                    Event domainEvent = JsonConvert.DeserializeObject<Event>(snsEventRecord.Sns.Message);
                    _repository.Handle(domainEvent);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
