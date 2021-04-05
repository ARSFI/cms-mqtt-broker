using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server.Configuration;
using MQTTnet.Server.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class CustomMqttFactory
    {
        private readonly MqttFactory _mqttFactory;

        public CustomMqttFactory(MqttSettingsModel settings, ILogger<MqttServer> logger)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
                _mqttFactory = new MqttFactory();

            Logger = _mqttFactory.DefaultLogger;
        }
        
        public IMqttNetLogger Logger { get; }

        public IMqttServer CreateMqttServer(List<IMqttServerAdapter> adapters)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));

            return _mqttFactory.CreateMqttServer(adapters);
        }
    }
}
