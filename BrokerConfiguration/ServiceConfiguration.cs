using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace winlink.cms.mqtt.config
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        public const int ConnectionDelayInMilliseconds = 5000;
        public const int StoppingDelayInMilliseconds = 2000;

        public ServiceConfiguration()
        {
            RemoteMqttBrokers = new List<RemoteMqttBroker>();
        }

        public string ClientId { get; set; }
        public int LocalMqttBrokerTcpPort { get; set; }
        public int LocalMqttBrokerWebSocketPort { get; set; }

        public List<RemoteMqttBroker> RemoteMqttBrokers { get; private set; }
    }

}
