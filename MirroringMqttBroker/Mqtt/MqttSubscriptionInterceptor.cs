using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
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
                _logger.LogInformation($"Received subscription for topic: {context.TopicFilter} from ClientId: {context.ClientId}");
                context.AcceptSubscription = true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting subscription.");
            }

            return Task.CompletedTask;
        }
    }
}
