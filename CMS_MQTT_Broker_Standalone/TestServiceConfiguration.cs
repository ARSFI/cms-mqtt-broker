using System.Collections.Generic;

namespace winlink.cms.mqtt.config
{
    public class TestServiceConfiguration : IServiceConfiguration
    {
        public TestServiceConfiguration()
        {
        }

        public string LocalClientId => "cms-a";
        public int LocalMqttBrokerTcpPort => 1883;
        public int LocalMqttBrokerWebSocketPort => 9001;
        public List<RemoteMqttBroker> RemoteMqttBrokers
        {
            get
            {
                var rm = new RemoteMqttBroker { Host = "localhost", Port = 1883 };
                return new List<RemoteMqttBroker> { rm };
            }
        }
    }
}
