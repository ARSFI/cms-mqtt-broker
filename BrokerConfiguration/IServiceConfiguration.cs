using System.Collections.Generic;

namespace winlink.cms.mqtt.config
{
    public interface IServiceConfiguration
    {
        // Our client ID for communication with the other broker.
        string LocalClientId { get; }

        // This broker's listening port.
        int LocalMqttBrokerTcpPort { get; }

        // This broker's websocket listening port.
        int LocalMqttBrokerWebSocketPort { get; }

        // The other broker(s) IP address/hostname, etc.
        public List<RemoteMqttBroker> RemoteMqttBrokers { get; }
    }
}
