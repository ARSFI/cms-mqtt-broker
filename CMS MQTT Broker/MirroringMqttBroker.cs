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
using winlink.cms.mqtt.config;

namespace winlink.cms.mqtt
{
    public class MirroringMqttBroker : BackgroundService
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        static IMqttClient mqttClient;

        private IServiceConfiguration serviceConfiguration;

        public MirroringMqttBroker(IServiceConfiguration configuration)
        {
            serviceConfiguration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create MQTT factory.
            var mqttFactory = new MqttFactory();

            // Create options
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(serviceConfiguration.ThisServicePort)
                .WithClientId(serviceConfiguration.ThisClientId)
                .WithApplicationMessageInterceptor((arg) =>
                {
                    arg.AcceptPublish = true;

                    // Avoid loops by not mirroring messages from other servers.
                    if (arg.ClientId != serviceConfiguration.ThisClientId)
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
                .WithClientId(serviceConfiguration.ThisClientId)
                .WithTcpServer(
                    serviceConfiguration.OtherServiceHostname,
                    serviceConfiguration.OtherServicePort);

            //!!! below must be able to sustain a disconnect and recover

            await Task.Delay(serviceConfiguration.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
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
                
                await Task.Delay(serviceConfiguration.StoppingDelayInMilliseconds, stoppingToken);
            }
            await mqttServer.StopAsync();
        }
    }
}

