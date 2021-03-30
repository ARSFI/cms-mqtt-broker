using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class MqttClientSubscribedTopicHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly ILogger _logger;

        public MqttClientSubscribedTopicHandler(ILogger<MqttClientSubscribedTopicHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            try
            {
                //TODO:
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client subscribed topic event.");
            }

            return Task.CompletedTask;
        }
    }
}
