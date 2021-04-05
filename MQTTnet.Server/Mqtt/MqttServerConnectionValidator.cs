using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerConnectionValidator : IMqttServerConnectionValidator
    {
        public const string WrappedSessionItemsKey = "WRAPPED_ITEMS";

        private readonly ILogger _logger;

        public MqttServerConnectionValidator(ILogger<MqttServerConnectionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
        {
            try
            {
                //TODO: If enabled in settings, verify username and password

                //if (!_serviceConfiguration.RequireClientAuthentication || connection.Username == _serviceConfiguration.LocalMqttBrokerUsername &&
                //    connection.Password == _serviceConfiguration.LocalMqttBrokerPassword)
                //{
                //    connection.ReasonCode = MqttConnectReasonCode.Success;
                //    Log.Info($"New connection - ClientId: {connection.ClientId}");
                //}
                //else
                //{
                //    connection.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                //    Log.Info($"Invalid connection attempt - ClientId: {connection.ClientId}, Username: {connection.Username}");
                //}

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
