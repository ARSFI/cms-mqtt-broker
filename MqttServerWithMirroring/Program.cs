using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;

namespace MqttServerWithMirroring
{
    class Program
    {
        static readonly string OurClientId = "MqttMirroringServer";
        static IMqttClient ourClient;

        static void PrintUsage(string appName)
        {
            Console.WriteLine("Usage: " + appName + " [ourPort] [theirIpAndPort]");
        }

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage(args[0]);
                return;
            }

            string ourPortAsString = args[1];
            string[] theirIpAndPort = args[2].Split(':');
            int ourPort = 1883;
            int theirPort = 1883;

            if (!int.TryParse(ourPortAsString, out ourPort))
            {
                Console.WriteLine("Can't parse ourPort: " + ourPortAsString);
                PrintUsage(args[0]);
                return;
            }

            if (theirIpAndPort.Length < 2)
            {
                Console.WriteLine("theirIpAndPort must be two fields separated by a colon (e.g. localhost:8888). Provided: " + args[2]);
                PrintUsage(args[0]);
                return;
            }

            if (!int.TryParse(theirIpAndPort[1], out theirPort))
            {
                Console.WriteLine("Can't parse port component of theirIpAndPort. Provided: " + theirIpAndPort[1]);
                PrintUsage(args[0]);
                return;
            }

            // Create MQTT factory.
            var mqttFactory = new MqttFactory();

            // Create options
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(ourPort)
                .WithClientId(OurClientId)
                .WithApplicationMessageInterceptor((arg) =>
                {
                    arg.AcceptPublish = true;

                    // Avoid loops by not mirroring messages from other servers.
                    if (arg.ClientId != OurClientId)
                    {
                        // Mirror message on other server.
                        ourClient.PublishAsync(arg.ApplicationMessage);
                    }
                });

            // Start a MQTT server.
            var mqttServer = mqttFactory.CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());

            // Connect to the other server after a delay.
            ourClient = mqttFactory.CreateMqttClient();
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(OurClientId)
                .WithTcpServer(theirIpAndPort[0], theirPort);

            await Task.Delay(5000).ContinueWith( async (arg) =>
            {
                await ourClient.ConnectAsync(mqttClientOptionsBuilder.Build());
            });

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            await mqttServer.StopAsync();
        }
    }
}
