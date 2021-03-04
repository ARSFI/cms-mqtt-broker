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
            if (!WindowsServiceHelpers.IsWindowsService())
            {
                Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");
            }

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
            mqttClient.UseConnectedHandler((eventArgs) =>
            {
                Console.WriteLine("Connected to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
            });

            mqttClient.UseDisconnectedHandler(async (eventArgs) =>
            {
                await Task.Delay(serviceConfiguration.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                {
                    // Reconnect on disconnect.
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Reconnecting to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
                        await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
                    }
                });
            });

            Console.WriteLine("Waiting " + serviceConfiguration.ConnectionDelayInMilliseconds.ToString() + " milliseconds before client connect");
            await Task.Delay(serviceConfiguration.ConnectionDelayInMilliseconds);
            Console.WriteLine("Connecting to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
            await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(serviceConfiguration.StoppingDelayInMilliseconds, stoppingToken);
            }

            mqttClient.DisconnectedHandler = null;
            await mqttServer.StopAsync();
            await mqttClient.DisconnectAsync();
        }
    }
}

