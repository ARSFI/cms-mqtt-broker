using System;
using System.Threading;
using System.Threading.Tasks;
using winlink.cms.mqtt;
using winlink.cms.mqtt.config;

namespace CMS_MQTT_Broker_Standalone
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var broker = new MirroringMqttBroker(new ServiceConfigurationFactory().GetConfiguration());
            var cancelToken = new CancellationToken();
            await broker.StartAsync(cancelToken);
        }
    }
}
