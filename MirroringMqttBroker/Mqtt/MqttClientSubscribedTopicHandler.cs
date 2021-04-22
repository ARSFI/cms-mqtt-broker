using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttClientSubscribedTopicHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly ILogger _logger;

        public MqttClientSubscribedTopicHandler(ILogger<MqttClientSubscribedTopicHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs context)
        {
            try
            {
                //_logger.LogInformation($"Received subscribe request from '{context.ClientId}' for topic '{context.TopicFilter.Topic}'");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while handling client subscribed topic event from '{context.ClientId}'");
            }

            return Task.CompletedTask;
        }
    }
}
