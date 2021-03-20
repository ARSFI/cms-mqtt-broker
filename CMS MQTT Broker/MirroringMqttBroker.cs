using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTTnet.Server;
using NLog;
using winlink.cms.mqtt.config;

namespace winlink.cms.mqtt
{
    public class MirroringMqttBroker : BackgroundService
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        //to monitor in real-time use Log2Console w/ udp receiver configured to listen on port 7071

        private List<IMqttClient> mqttClients;
        private List<IMqttClientOptions> mqttClientOptions;

        private readonly IServiceConfiguration serviceConfiguration;

        public MirroringMqttBroker(IServiceConfiguration configuration)
        {
            serviceConfiguration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!WindowsServiceHelpers.IsWindowsService())
            {
                Console.WriteLine($"Broker running at: {DateTimeOffset.Now}");
                _log.Info($"Broker running at: {DateTimeOffset.Now}");
            }

            // Create MQTT factory.
            var mqttFactory = new MqttFactory();

            //TODO: Need to implement websocket listener (port 9001)

            // Create options
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(serviceConfiguration.LocalMqttBrokerTcpPort)
                .WithClientId(serviceConfiguration.ClientId)
                .WithConnectionValidator(connection =>
                {
                    //TODO: Possibly implement basic auth -- not sure what type of credential store would be used

                    connection.ReasonCode = MqttConnectReasonCode.Success;

                    _log.Debug($"New connection - ClientId: {connection.ClientId}");
                })
                .WithApplicationMessageInterceptor((arg) =>
                {
                    arg.AcceptPublish = true;

                    // Avoid loops by not mirroring messages from other servers.
                    if (arg.ClientId != serviceConfiguration.ClientId)
                    {
                        foreach (var client in mqttClients)
                        {
                            if (client.IsConnected)
                            {
                                client.PublishAsync(arg.ApplicationMessage);
                            }
                        }
                    }
 
                    //TODO: Temporary
                    var payload = System.Text.Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
                    _log.Trace($"Received message : {arg.ApplicationMessage.Topic} / {payload}");
                });

            // Start a MQTT server.
            var mqttServer = mqttFactory.CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());

            // Create clients
            mqttClients = new List<IMqttClient>();
            mqttClientOptions = new List<IMqttClientOptions>();
            foreach (var clientConfig in serviceConfiguration.RemoteMqttBrokers)
            {
                var mqttClient = mqttFactory.CreateMqttClient();
                var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(serviceConfiguration.ClientId)
                    .WithTcpServer(
                        clientConfig.Host,
                        clientConfig.Port);
                mqttClients.Add(mqttClient);
                mqttClientOptions.Add(mqttClientOptionsBuilder.Build());

                //sustain a disconnect and recover
                mqttClient.UseConnectedHandler((eventArgs) =>
                {
                    Console.WriteLine("Connected to " + clientConfig.Host + " port " + clientConfig.Port);
                    _log.Info($"Connected to {clientConfig.Host} port {clientConfig.Port}");
                });

                mqttClient.UseDisconnectedHandler(async (eventArgs) =>
                {
                    await Task.Delay(ServiceConfiguration.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                    {
                        // Reconnect on disconnect.
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Reconnecting to " + clientConfig.Host + " port " + clientConfig.ToString());
                            _log.Debug($"Reconnecting to {clientConfig.Host} port {clientConfig.Port}");
                            await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
                        }
                    });
                });
            }

            Console.WriteLine("Waiting " + ServiceConfiguration.ConnectionDelayInMilliseconds.ToString() + " milliseconds before client connect");
            _log.Trace($"Waiting {ServiceConfiguration.ConnectionDelayInMilliseconds} milliseconds before client connect");
            await Task.Delay(ServiceConfiguration.ConnectionDelayInMilliseconds);

            // Connect to clients
            int index = 0;
            foreach (var clientConfig in serviceConfiguration.RemoteMqttBrokers)
            {
                Console.WriteLine("Connecting to " + clientConfig.Host + " port " + clientConfig.Port.ToString());
                _log.Info($"Connecting to {clientConfig.Host} port {clientConfig.Port}");
                await mqttClients[index].ConnectAsync(mqttClientOptions[index]);
                index++;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(ServiceConfiguration.StoppingDelayInMilliseconds, stoppingToken);
            }

            // Disconnect from clients
            index = 0;
            foreach (var clientConfig in serviceConfiguration.RemoteMqttBrokers)
            {
                mqttClients[index].DisconnectedHandler = null;
                await mqttClients[index].DisconnectAsync();
                index++;
            }

            // Stop listening for requests.
            await mqttServer.StopAsync();
        }
    }
}

