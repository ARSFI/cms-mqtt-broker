using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using mirroring.mqtt.broker.config;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTTnet.Server;
using NLog;

namespace mirroring.mqtt.broker
{
    public class MirroringMqttBroker : BackgroundService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private List<IMqttClient> _mqttClients;
        private List<IMqttClientOptions> _mqttClientOptions;
        private readonly IBrokerConfiguration _serviceConfiguration;

        public MirroringMqttBroker(IBrokerConfiguration configuration)
        {
            _serviceConfiguration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!WindowsServiceHelpers.IsWindowsService())
            {
                Console.WriteLine($"MQTT Broker running at: {DateTimeOffset.Now}");
            }
            Log.Info($"MQTT Broker running at: {DateTimeOffset.Now}");

            // Create MQTT factory.
            var mqttFactory = new MqttFactory();

            //TODO: Need to implement websocket listener

            // Create options
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(_serviceConfiguration.LocalMqttBrokerTcpPort)
                .WithClientId(_serviceConfiguration.ClientId)
                .WithConnectionValidator(connection =>
                {
                    //TODO: Possibly implement basic auth -- not sure what type of credential store would be used
                    connection.ReasonCode = MqttConnectReasonCode.Success;
                    Log.Debug($"New connection - ClientId: {connection.ClientId}");
                })
                .WithApplicationMessageInterceptor((arg) =>
                {
                    arg.AcceptPublish = true;

                    // Avoid loops by not mirroring messages from other servers.
                    if (arg.ClientId != _serviceConfiguration.ClientId)
                    {
                        foreach (var client in _mqttClients)
                        {
                            if (client.IsConnected)
                            {
                                client.PublishAsync(arg.ApplicationMessage);
                            }
                        }
                    }

                    //TODO: Temporary
                    var payload = System.Text.Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
                    Log.Debug($"Received message : {arg.ApplicationMessage.Topic} / {payload}");
                });

            // Start a MQTT server.
            var mqttServer = mqttFactory.CreateMqttServer();
            await mqttServer.StartAsync(optionsBuilder.Build());

            // Create clients
            _mqttClients = new List<IMqttClient>();
            _mqttClientOptions = new List<IMqttClientOptions>();
            foreach (var clientConfig in _serviceConfiguration.RemoteMqttBrokers)
            {
                var mqttClient = mqttFactory.CreateMqttClient();
                var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(_serviceConfiguration.ClientId)
                    .WithTcpServer(
                        clientConfig.Host,
                        clientConfig.Port);
                _mqttClients.Add(mqttClient);
                _mqttClientOptions.Add(mqttClientOptionsBuilder.Build());

                mqttClient.UseConnectedHandler((eventArgs) =>
                {
                    Console.WriteLine("Connected to " + clientConfig.Host + " port " + clientConfig.Port);
                    Log.Info($"Connected to {clientConfig.Host} port {clientConfig.Port}");
                });

                // Sustain a disconnect and recover
                mqttClient.UseDisconnectedHandler(async (eventArgs) =>
                {
                    await Task.Delay(BrokerConfiguration.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                    {
                        // Reconnect on disconnect.
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Reconnecting to " + clientConfig.Host + " port " + clientConfig);
                            Log.Debug($"Reconnecting to {clientConfig.Host} port {clientConfig.Port}");
                            await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
                        }
                    });
                });
            }

            Console.WriteLine("Waiting " + BrokerConfiguration.ConnectionDelayInMilliseconds.ToString() + " milliseconds before client connect");
            Log.Trace($"Waiting {BrokerConfiguration.ConnectionDelayInMilliseconds} milliseconds before client connect");
            await Task.Delay(BrokerConfiguration.ConnectionDelayInMilliseconds);

            // Connect to clients
            int index = 0;
            foreach (var clientConfig in _serviceConfiguration.RemoteMqttBrokers)
            {
                Console.WriteLine("Connecting to " + clientConfig.Host + " port " + clientConfig.Port.ToString());
                Log.Info($"Connecting to {clientConfig.Host} port {clientConfig.Port}");
                await _mqttClients[index].ConnectAsync(_mqttClientOptions[index]);
                index++;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(BrokerConfiguration.StoppingDelayInMilliseconds, stoppingToken);
            }

            // Disconnect from clients
            index = 0;
            foreach (var clientConfig in _serviceConfiguration.RemoteMqttBrokers)
            {
                _mqttClients[index].DisconnectedHandler = null;
                await _mqttClients[index].DisconnectAsync();
                index++;
            }

            // Stop listening for requests.
            await mqttServer.StopAsync();
        }
    }
}

