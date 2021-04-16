using System;
using System.Collections.Generic;
using System.Linq;
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

        public void SetServerService(IMqttServerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            try
            {
                context.AcceptPublish = true;

                // Avoid loops by not mirroring messages from remote brokers.
                if (_service.Settings.RemoteBrokers.Any(remoteBroker => context.ClientId == remoteBroker.ClientId))
                {
                    return Task.CompletedTask;
                }

                foreach (var client in _service.Clients)
                {
                    if (client.IsConnected)
                    {
                        // Find topic filters for this connection. Default to match all topics
                        var topicFilters = new List<string> {"#"}; 
                        foreach (var remoteBroker in _service.Settings.RemoteBrokers)
                        {
                            if (remoteBroker.ClientId == client.Options.ClientId)
                            {
                                topicFilters = remoteBroker.TopicFilters;
                            }
                        }

                        // Publish only matching topics
                        foreach (var filter in topicFilters)
                        {
                            if (MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, filter))
                            {
                                client.PublishAsync(context.ApplicationMessage);
                                // Should only send once even if topic matches multiple topic filters
                                break;
                            }
                        }
                    }
                }

                ////TODO: Temporary
                var payload = System.Text.Encoding.UTF8.GetString(context.ApplicationMessage.Payload);
                _logger.LogDebug($"Received message : {context.ApplicationMessage.Topic} / {payload}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting application message.");
            }

            return Task.CompletedTask;
        }
    }
}