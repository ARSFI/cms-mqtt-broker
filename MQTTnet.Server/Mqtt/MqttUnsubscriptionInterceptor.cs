using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class MqttUnsubscriptionInterceptor : IMqttServerUnsubscriptionInterceptor
    {
        private readonly ILogger _logger;

        public MqttUnsubscriptionInterceptor(ILogger<MqttUnsubscriptionInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptUnsubscriptionAsync(MqttUnsubscriptionInterceptorContext context)
        {
            try
            {
                _logger.LogInformation($"Received unsubscribe request from '{context.ClientId}' for topic: '{context.Topic}'");
                context.AcceptUnsubscription = true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while handling unsubscribe request from '{context.ClientId}' for topic '{context.Topic}'");
            }

            return Task.CompletedTask;
        }
    }
}
