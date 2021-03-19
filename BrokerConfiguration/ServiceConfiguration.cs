using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace winlink.cms.mqtt.config
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        public const int ConnectionDelayInMilliseconds = 5000;
        public const int StoppingDelayInMilliseconds = 2000;

        private readonly IConfigurationRoot _config;

        public ServiceConfiguration(IConfigurationRoot configuration)
        {
            _config = configuration;
        }

        public string LocalClientId => Convert.ToString(_config["clientId"]);
        public int LocalMqttBrokerTcpPort => Convert.ToInt32(_config["localMqttBrokerTcpPort"]);
        public int LocalMqttBrokerWebSocketPort => Convert.ToInt32(_config["localMqttBrokerWebSocketPort"]);

        
        //TODO: Haven't figured out how to read this from appsettings.json !!!

        public List<RemoteMqttBroker> RemoteMqttBrokers { get; }
    }

}
