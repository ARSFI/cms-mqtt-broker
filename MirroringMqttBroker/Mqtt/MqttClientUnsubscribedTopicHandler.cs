using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttClientUnsubscribedTopicHandler : IMqttServerClientUnsubscribedTopicHandler
    {
        private readonly ILogger _logger;

        public MqttClientUnsubscribedTopicHandler(ILogger<MqttClientUnsubscribedTopicHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientUnsubscribedTopicAsync(MqttServerClientUnsubscribedTopicEventArgs context)
        {
            try
            {
                _logger.LogInformation($"Received unsubscribe request from '{context.ClientId}' for topic '{context.TopicFilter}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client unsubscribed topic event.");
            }

            return Task.CompletedTask;
        }
    }
}
