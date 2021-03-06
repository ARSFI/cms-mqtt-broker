using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttClientDisconnectedHandler : IMqttServerClientDisconnectedHandler
    {
        private readonly ILogger _logger;

        public MqttClientDisconnectedHandler(ILogger<MqttClientDisconnectedHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs context)
        {
            try
            {
                _logger.LogInformation($"Client disconnected: {context.ClientId}, Disconnect Type: {context.DisconnectType}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while handling client disconnected event.");
            }

            return Task.CompletedTask;
        }
    }
}
