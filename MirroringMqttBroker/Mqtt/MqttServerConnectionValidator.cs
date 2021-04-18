using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MirroringMqttBroker.Configuration;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttServerConnectionValidator : IMqttServerConnectionValidator
    {
        private readonly ILogger _logger;
        private readonly MqttSettingsModel _mqttSettingsModel;

        public MqttServerConnectionValidator(MqttSettingsModel mqttSettingsModel, ILogger<MqttServerConnectionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mqttSettingsModel = mqttSettingsModel ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
        {
            try
            {
                if (!_mqttSettingsModel.RequireClientAuthentication)
                {
                    context.ReasonCode = MqttConnectReasonCode.Success;
                    _logger.LogInformation($"New connection - ClientId: {context.ClientId}");
                    return Task.CompletedTask;
                }

                // Search for matching credentials
                foreach (var clientCredential in _mqttSettingsModel.ClientCredentials)
                {
                    if (context.Username != null && context.Username.Equals(clientCredential.UserName, 
                        StringComparison.OrdinalIgnoreCase) && context.Password == clientCredential.Password)
                    {
                        context.ReasonCode = MqttConnectReasonCode.Success;
                        _logger.LogInformation($"New validated connection - ClientId: {context.ClientId}");
                        return Task.CompletedTask;
                    }
                }

                // Otherwise, reject connection
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                _logger.LogWarning($"Invalid connection attempt - ClientId: {context.ClientId}, Username: {context.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while validating client connection.");
                context.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
            }

            return Task.CompletedTask;
        }
    }
}
