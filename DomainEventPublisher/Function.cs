using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Domain;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace domain_event_publisher
{
    /// <summary>
    /// Processes DynamoDB streams and then broadcast them via SNS.
    /// </summary>
    public class Function
    {
        private readonly AmazonSimpleNotificationServiceClient _simpleNotificationServiceClient;
        private readonly string _snsBaseArn;

        public Function()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            _snsBaseArn = "arn:aws:sns:" + Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") + 
                ":" + Environment.GetEnvironmentVariable("ACCOUNT") + ":";

            _simpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient();
        }

        public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            Console.WriteLine("Received DynamoDBEvent" + JsonConvert.SerializeObject(dynamoEvent));

            try
            {
                foreach (var record in dynamoEvent.Records)
                    if (record.EventName == OperationType.INSERT)
                    {
                        var attributeMap = record.Dynamodb.NewImage;
                        if (attributeMap.Count > 0) // If item does not exist, attributeMap.Count will be 0
                        {
                            var changesetContent = attributeMap["ChangesetContent"].S;

                            List<Event> domainEvents = GetEvents(changesetContent);
                            foreach (var domainEvent in domainEvents)
                            {
                                await BroadcastDomainEvent(domainEvent);
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
            }
        }

        private async Task BroadcastDomainEvent(Event domainEvent)
        {
            if (domainEvent == null) return;
            string eventName = domainEvent.GetType().Name;

            string topic = _snsBaseArn + eventName;
            Console.WriteLine("SNS topic arn: " + topic);

            string json = JsonConvert.SerializeObject(domainEvent);

            PublishRequest publishRequest = new PublishRequest
            {
                Message = json,
                TopicArn = topic
            };
            PublishResponse response = await _simpleNotificationServiceClient.PublishAsync(publishRequest);
            Console.WriteLine("Broadcasted domain event: " + eventName + " with MessageId: " + response.MessageId);
        }

        private List<Event> GetEvents(string changesetContent)
        {
            Console.WriteLine("Getting events for changesetContent: " + changesetContent);

            List<Event> events = new List<Event>();
            events = JsonConvert.DeserializeObject<List<Event>>(changesetContent);

            return events;
        }
    }
}
