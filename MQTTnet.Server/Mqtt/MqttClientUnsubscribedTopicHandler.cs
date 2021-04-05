using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientUnsubscribedTopicHandler : IMqttServerClientUnsubscribedTopicHandler
    {
        private readonly ILogger _logger;

        public MqttClientUnsubscribedTopicHandler(ILogger<MqttClientUnsubscribedTopicHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientUnsubscribedTopicAsync(MqttServerClientUnsubscribedTopicEventArgs eventArgs)
        {
            try
            {
                //TODO:

                _logger.LogInformation($"{eventArgs.ClientId} unsubscribed from: {eventArgs.TopicFilter}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client unsubscribed topic event.");
            }

            return Task.CompletedTask;
        }
    }
}
