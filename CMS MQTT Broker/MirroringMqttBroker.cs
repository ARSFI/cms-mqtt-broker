using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;

namespace winlink.cms.mqtt
{
    public class MirroringMqttBroker : BackgroundService
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        static IMqttClient mqttClient;

        //!!! move ip,port, and client id's to configuration class
        private int ourPort = 1883; //this will need to be added to the global config data in mysql 
        private int theirPort = 1883; //this will need to be added to the global config data in mysql 
        private string OurClientId = "cms-a"; //this can be obtained from each cms and passed as part of the configuration
        private string theirIp = ""; //this will need to be added to the global config data in mysql 
        private int connectionDelayInMilliseconds = 5000;
        private int stoppingDelayInMilliseconds = 2000;

        public MirroringMqttBroker()
        {
            //
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
                        mqttClient.PublishAsync(arg.ApplicationMessage);
                    }
                });

            // Start a MQTT server.
            var mqttServer = mqttFactory.CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());
            
            // Connect to the other server after a delay.
            mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(OurClientId)
                .WithTcpServer(theirIp, theirPort);

            //!!! below must be able to sustain a disconnect and recover

            await Task.Delay(connectionDelayInMilliseconds).ContinueWith(async (arg) =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
                }
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!WindowsServiceHelpers.IsWindowsService())
                {
                    Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");
                }

                //!!!
                
                await Task.Delay(stoppingDelayInMilliseconds, stoppingToken);
            }
            await mqttServer.StopAsync();
        }
    }
}

