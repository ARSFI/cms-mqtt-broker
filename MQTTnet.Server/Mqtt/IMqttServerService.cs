using System;
using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Server.Configuration;

namespace MQTTnet.Server.Mqtt
{
    public interface IMqttServerService
    {
        // We return IEnumerable here so callers don't inadvertently modify the collection.
        IEnumerable<IMqttClient> Clients { get; }

        MqttSettingsModel Settings { get; }
    }
}
