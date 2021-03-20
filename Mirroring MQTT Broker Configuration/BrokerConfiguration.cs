using System.Collections.Generic;

namespace mirroring.mqtt.broker.config
{
    public class BrokerConfiguration : IBrokerConfiguration
    {
        public const int ConnectionDelayInMilliseconds = 5000;
        public const int StoppingDelayInMilliseconds = 2000;

        public BrokerConfiguration()
        {
            RemoteMqttBrokers = new List<RemoteBrokerConfiuration>();
        }

        public string ClientId { get; set; }
        public int LocalMqttBrokerTcpPort { get; set; }
        public int LocalMqttBrokerWebSocketPort { get; set; }

        public List<RemoteBrokerConfiuration> RemoteMqttBrokers { get; private set; }
    }

}
