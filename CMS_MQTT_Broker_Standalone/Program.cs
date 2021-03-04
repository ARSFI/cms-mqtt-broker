using System;
using System.Threading;
using System.Threading.Tasks;
using winlink.cms.mqtt;
using winlink.cms.mqtt.config;
using winlink.cms.data;

namespace CMS_MQTT_Broker_Standalone
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: {0} [SQL Config String]", args[0]);
                return;
            }

            var database = new CMSDatabase(args[1]);
            var propertiesTable = new CMSProperties(database);
            var broker = new MirroringMqttBroker(new DatabaseServiceConfiguration(propertiesTable));
            var cancelToken = new CancellationToken();
            await broker.StartAsync(cancelToken);

            Console.WriteLine("Press Enter to quit.");
            _ = Console.ReadLine();
        }
    }
}
