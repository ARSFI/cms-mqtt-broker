using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class MqttApplicationMessageInterceptor : IMqttServerApplicationMessageInterceptor
    {
        private readonly ILogger _logger;

        public MqttApplicationMessageInterceptor(ILogger<MqttApplicationMessageInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            try
            {
                // This might be not set when a message was published by the server instead of a client.
                context.SessionItems.TryGetValue(MqttServerConnectionValidator.WrappedSessionItemsKey, out var sessionItems);

                //TODO:

                //arg.AcceptPublish = true;

                //// Avoid loops by not mirroring messages from other servers.
                //if (arg.ClientId != _serviceConfiguration.ClientId)
                //{
                //    foreach (var client in _mqttClients)
                //    {
                //        if (client.IsConnected)
                //        {
                //            client.PublishAsync(arg.ApplicationMessage);
                //        }
                //    }
                //}

                ////TODO: Temporary
                //var payload = System.Text.Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
                //Log.Debug($"Received message : {arg.ApplicationMessage.Topic} / {payload}");

            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting application message.");
            }

            return Task.CompletedTask;
        }
    }
}