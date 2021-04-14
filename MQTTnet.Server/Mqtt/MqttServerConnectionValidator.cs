using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server.Configuration;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerConnectionValidator : IMqttServerConnectionValidator
    {
        public const string WrappedSessionItemsKey = "WRAPPED_ITEMS";

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
                if (!_mqttSettingsModel.RequireClientAuthentication || context.Username == _mqttSettingsModel.BrokerUsername &&
                    context.Password == _mqttSettingsModel.BrokerPassword)
                {
                    context.ReasonCode = MqttConnectReasonCode.Success;
                    _logger.LogInformation($"New connection - ClientId: {context.ClientId}");
                }
                else
                {
                    context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                    _logger.LogInformation($"Invalid connection attempt - ClientId: {context.ClientId}, Username: {context.Username}");
                }

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
