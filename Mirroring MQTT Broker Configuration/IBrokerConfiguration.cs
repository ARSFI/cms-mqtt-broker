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

        // Flag to control use of client authentication
        bool RequireAuthentication { get; }

        // The username to use to connect to this broker.
        string LocalMqttBrokerUsername { get; }

        // The password to use to connect to this broker.
        string LocalMqttBrokerPassword { get; }

        // The other broker(s) IP address/hostname, etc.
        public List<RemoteBrokerConfiguration> RemoteMqttBrokers { get; }
    }
}
