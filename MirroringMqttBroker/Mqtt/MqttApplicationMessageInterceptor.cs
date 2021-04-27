using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttApplicationMessageInterceptor : IMqttServerApplicationMessageInterceptor
    {
        private readonly ILogger _logger;
        private IMqttServerService _service;

        /// <summary>
        /// Used as CorrelationData when forwarding a message to other brokers to prevent message forwarding loops
        /// </summary>
        private static readonly byte[] ForwardedSignature = { 13, 7, 13, 7, 13 };


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

                // Avoid loops by not mirroring messages that have been forwarded by other brokers.
                if (context.ApplicationMessage.CorrelationData != null && context.ApplicationMessage.CorrelationData.SequenceEqual(ForwardedSignature))
                {
                    _logger.LogTrace($"Detected already forwarded message. Remote Client ID: {context.ClientId}, Topic {context.ApplicationMessage.Topic}");
                    return Task.CompletedTask;
                }

                foreach (var broker in _service.RemoteBrokers)
                {
                    var brokerName = "";
                    if (broker.IsConnected)
                    {
                        // Find topic filters for this connection. Default to match all topics
                        var topicFilters = new List<string> { "#" };
                        foreach (var remoteBroker in _service.Settings.RemoteBrokers)
                        {
                            // Remote brokers prefix their client id with [broker_name]
                            if (broker.Options.ClientId.EndsWith(remoteBroker.ClientId))
                            {
                                topicFilters = remoteBroker.TopicFilters;
                                brokerName = remoteBroker.Name;
                            }
                        }

                        // Publish only matching topics
                        foreach (var filter in topicFilters)
                        {
                            if (MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, filter))
                            {
                                _logger.LogTrace($"Forwarded message with topic: '{context.ApplicationMessage.Topic}' to remote broker: {brokerName}");

                                // Include forwarding signature to flag message as forwarded
                                context.ApplicationMessage.CorrelationData = ForwardedSignature;
                                broker.PublishAsync(context.ApplicationMessage);

                                // Should only send once even if topic matches multiple topic filters
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while intercepting application message.");
            }

            return Task.CompletedTask;
        }
    }
}