using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class MqttSubscriptionInterceptor : IMqttServerSubscriptionInterceptor
    {
        private readonly ILogger _logger;

        public MqttSubscriptionInterceptor(ILogger<MqttSubscriptionInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
        {
            try
            {
                //TODO:
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting subscription.");
            }

            return Task.CompletedTask;
        }
    }
}