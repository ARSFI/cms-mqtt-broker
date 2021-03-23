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
        public string LocalMqttBrokerUsername => "testuser";
        public string LocalMqttBrokerPassword => "testpassword";

        public List<RemoteBrokerConfiguration> RemoteMqttBrokers
        {
            get
            {
                var rm = new RemoteBrokerConfiguration { Host = "localhost", Port = 1883 };
                return new List<RemoteBrokerConfiguration> { rm };
            }
        }
    }
}
