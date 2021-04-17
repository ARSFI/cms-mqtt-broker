using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttClientConnectedHandler : IMqttServerClientConnectedHandler
    {
        private readonly ILogger _logger;

        public MqttClientConnectedHandler(ILogger<MqttClientConnectedHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs context)
        {
            try
            {
                _logger.LogInformation($"Client connected: {context.ClientId}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client connected event.");
            }

            return Task.CompletedTask;
        }
    }
}
