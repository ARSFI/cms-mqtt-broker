using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using Microsoft.Extensions.Hosting.WindowsServices;
using MQTTnet.Protocol;
using NLog;
using winlink.cms.mqtt.config;

namespace winlink.cms.mqtt
{
    public class MirroringMqttBroker : BackgroundService
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        //to monitor in real-time use Log2Console w/ udp receiver configured to listen on port 7071

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
                Console.WriteLine($"Broker running at: {DateTimeOffset.Now}");
                _log.Info($"Broker running at: {DateTimeOffset.Now}");
            }

            // Create MQTT factory.
            var mqttFactory = new MqttFactory();

            //TODO: Need to implement websocket listener (port 9001)

            // Create options
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(serviceConfiguration.ThisServicePort)
                .WithClientId(serviceConfiguration.ThisClientId)
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
                    if (mqttClient.IsConnected &&  arg.ClientId != serviceConfiguration.ThisClientId)
                    {
                        // Mirror message on other server.
                        mqttClient.PublishAsync(arg.ApplicationMessage);
                    }

                    //TODO: Temporary
                    var payload = System.Text.Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
                    _log.Trace($"Received message : {arg.ApplicationMessage.Topic} / {payload}");
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

            //sustain a disconnect and recover
            mqttClient.UseConnectedHandler((eventArgs) =>
            {
                Console.WriteLine("Connected to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
                _log.Info($"Connected to {serviceConfiguration.OtherServiceHostname} port {serviceConfiguration.OtherServicePort}");
            });

            mqttClient.UseDisconnectedHandler(async (eventArgs) =>
            {
                await Task.Delay(serviceConfiguration.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                {
                    // Reconnect on disconnect.
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Reconnecting to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
                        _log.Debug($"Reconnecting to {serviceConfiguration.OtherServiceHostname} port {serviceConfiguration.OtherServicePort}");
                        await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build(), stoppingToken);
                    }
                });
            });

            Console.WriteLine("Waiting " + serviceConfiguration.ConnectionDelayInMilliseconds.ToString() + " milliseconds before client connect");
            _log.Trace($"Waiting {serviceConfiguration.ConnectionDelayInMilliseconds} milliseconds before client connect");
            await Task.Delay(serviceConfiguration.ConnectionDelayInMilliseconds);
            Console.WriteLine("Connecting to " + serviceConfiguration.OtherServiceHostname + " port " + serviceConfiguration.OtherServicePort.ToString());
            _log.Info($"Connecting to {serviceConfiguration.OtherServiceHostname} port {serviceConfiguration.OtherServicePort}");
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

