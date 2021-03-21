using System.Collections.Generic;

namespace mirroring.mqtt.broker.config
{
    public interface IBrokerConfiguration
    {
        // Our client ID for communication with the other broker.
        string ClientId { get; }

        // This broker's listening port.
        int LocalMqttBrokerTcpPort { get; }

        // This broker's websocket listening port.
        int LocalMqttBrokerWebSocketPort { get; }

        // The other broker(s) IP address/hostname, etc.
        public List<RemoteBrokerConfiguration> RemoteMqttBrokers { get; }
    }
}
