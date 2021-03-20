using System.Collections.Generic;
using mirroring.mqtt.broker.config;

namespace CMS_MQTT_Broker_Standalone
{
    public class TestServiceConfiguration : IBrokerConfiguration
    {
        public TestServiceConfiguration()
        {
        }

        public string ClientId => "CMS-A";
        public int LocalMqttBrokerTcpPort => 1883;
        public int LocalMqttBrokerWebSocketPort => 9001;
        public List<RemoteBrokerConfiuration> RemoteMqttBrokers
        {
            get
            {
                var rm = new RemoteBrokerConfiuration { Host = "localhost", Port = 1883 };
                return new List<RemoteBrokerConfiuration> { rm };
            }
        }
    }
}
