using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace MQTTnet.Server.Mqtt
{
    public class MqttApplicationMessageInterceptor : IMqttServerApplicationMessageInterceptor
    {
        private readonly ILogger _logger;
        private IMqttServerService _service;

        public MqttApplicationMessageInterceptor(ILogger<MqttApplicationMessageInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void setServerService(IMqttServerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            try
            {
                // This might be not set when a message was published by the server instead of a client.
                context.SessionItems.TryGetValue(MqttServerConnectionValidator.WrappedSessionItemsKey, out var sessionItems);

                context.AcceptPublish = true;

                // Avoid loops by not mirroring messages from other servers.
                if (context.ClientId != _service.Settings.BrokerClientId)
                {
                    foreach (var client in _service.Clients)
                    {
                        if (client.IsConnected)
                        {
                            client.PublishAsync(context.ApplicationMessage);
                        }
                    }
                }

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