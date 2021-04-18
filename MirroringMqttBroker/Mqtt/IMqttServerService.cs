using System.Collections.Generic;
using MirroringMqttBroker.Configuration;
using MQTTnet.Client;

namespace MirroringMqttBroker.Mqtt
{
    public interface IMqttServerService
    {
        // We return IEnumerable here so callers don't inadvertently modify the collection.
        IEnumerable<IMqttClient> RemoteBrokers { get; }

        MqttSettingsModel Settings { get; }
    }
}
