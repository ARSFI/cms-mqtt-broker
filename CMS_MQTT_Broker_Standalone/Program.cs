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
            //if (args.Length < 2)
            //{
            //    Console.WriteLine("Usage: {0} [SQL Config String]", args[0]);
            //    return;
            //}

            var broker = new MirroringMqttBroker(new TestServiceConfiguration());
            var cancelToken = new CancellationToken();
            await broker.StartAsync(cancelToken);

            Console.WriteLine("Press Enter to quit.");
            _ = Console.ReadLine();
        }
    }
}
