using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Implementations;
using MQTTnet.Server.Configuration;
using NLog.Fluent;

namespace MQTTnet.Server.Mqtt
{
    public class MqttServerService 
    {
        private readonly ILogger<MqttServerService> _logger;

        readonly MqttSettingsModel _settings;
        readonly MqttApplicationMessageInterceptor _mqttApplicationMessageInterceptor;
        readonly MqttClientConnectedHandler _mqttClientConnectedHandler;
        readonly MqttClientDisconnectedHandler _mqttClientDisconnectedHandler;
        readonly MqttClientSubscribedTopicHandler _mqttClientSubscribedTopicHandler;
        readonly MqttClientUnsubscribedTopicHandler _mqttClientUnsubscribedTopicHandler;
        readonly MqttServerConnectionValidator _mqttConnectionValidator;
        readonly IMqttServer _mqttServer;
        readonly MqttSubscriptionInterceptor _mqttSubscriptionInterceptor;
        readonly MqttUnsubscriptionInterceptor _mqttUnsubscriptionInterceptor;
        readonly MqttWebSocketServerAdapter _webSocketServerAdapter;

        private List<IMqttClient> _mqttClients;
        private List<IMqttClientOptions> _mqttClientOptions;

        public MqttServerService(
            MqttSettingsModel mqttSettings,
            MqttClientConnectedHandler mqttClientConnectedHandler,
            MqttClientDisconnectedHandler mqttClientDisconnectedHandler,
            MqttClientSubscribedTopicHandler mqttClientSubscribedTopicHandler,
            MqttClientUnsubscribedTopicHandler mqttClientUnsubscribedTopicHandler,
            MqttServerConnectionValidator mqttConnectionValidator,
            MqttSubscriptionInterceptor mqttSubscriptionInterceptor,
            MqttUnsubscriptionInterceptor mqttUnsubscriptionInterceptor,
            MqttApplicationMessageInterceptor mqttApplicationMessageInterceptor,
            ILogger<MqttServerService> logger)
        {
            _settings = mqttSettings ?? throw new ArgumentNullException(nameof(mqttSettings));
            _mqttClientConnectedHandler = mqttClientConnectedHandler ?? throw new ArgumentNullException(nameof(mqttClientConnectedHandler));
            _mqttClientDisconnectedHandler = mqttClientDisconnectedHandler ?? throw new ArgumentNullException(nameof(mqttClientDisconnectedHandler));
            _mqttClientSubscribedTopicHandler = mqttClientSubscribedTopicHandler ?? throw new ArgumentNullException(nameof(mqttClientSubscribedTopicHandler));
            _mqttClientUnsubscribedTopicHandler = mqttClientUnsubscribedTopicHandler ?? throw new ArgumentNullException(nameof(mqttClientUnsubscribedTopicHandler));
            _mqttConnectionValidator = mqttConnectionValidator ?? throw new ArgumentNullException(nameof(mqttConnectionValidator));
            _mqttSubscriptionInterceptor = mqttSubscriptionInterceptor ?? throw new ArgumentNullException(nameof(mqttSubscriptionInterceptor));
            _mqttUnsubscriptionInterceptor = mqttUnsubscriptionInterceptor ?? throw new ArgumentNullException(nameof(mqttUnsubscriptionInterceptor));
            _mqttApplicationMessageInterceptor = mqttApplicationMessageInterceptor ?? throw new ArgumentNullException(nameof(mqttApplicationMessageInterceptor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var mqttFactory = new MqttFactory();
            _webSocketServerAdapter = new MqttWebSocketServerAdapter(mqttFactory.DefaultLogger);
            var adapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(mqttFactory.DefaultLogger)
                {
                    TreatSocketOpeningErrorAsWarning = true
                },
                _webSocketServerAdapter
            };

            _mqttServer = mqttFactory.CreateMqttServer(adapters);
        }

        // We return IEnumerable here so callers don't inadvertently modify the collection.
        public IEnumerable<IMqttClient> Clients {  get { return _mqttClients; } }
        
        public MqttSettingsModel Settings { get { return _settings; } }

        public void Configure()
        {
            _mqttServer.ClientConnectedHandler = _mqttClientConnectedHandler;
            _mqttServer.ClientDisconnectedHandler = _mqttClientDisconnectedHandler;
            _mqttServer.ClientSubscribedTopicHandler = _mqttClientSubscribedTopicHandler;
            _mqttServer.ClientUnsubscribedTopicHandler = _mqttClientUnsubscribedTopicHandler;

            _mqttServer.StartAsync(CreateMqttServerOptions()).GetAwaiter().GetResult();
            _logger.LogInformation("MQTT server started.");

            // Connect to remote brokers 
            _mqttClients = new List<IMqttClient>();
            _mqttClientOptions = new List<IMqttClientOptions>();
            var mqttFactory = new MqttFactory();

            foreach (var clientConfig in _settings.RemoteBrokers)
            {
                var mqttClient = mqttFactory.CreateMqttClient();
                var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(_settings.BrokerClientId)
                    .WithTcpServer(
                        clientConfig.Host,
                        clientConfig.Port);
                _mqttClients.Add(mqttClient);
                _mqttClientOptions.Add(mqttClientOptionsBuilder.Build());

                mqttClient.UseConnectedHandler((eventArgs) =>
                {
                    _logger.LogInformation($"Connected to {clientConfig.Host} port {clientConfig.Port}");
                });

                // Sustain a disconnect and reconnect
                mqttClient.UseDisconnectedHandler(async (eventArgs) =>
                {
                    await Task.Delay(MqttSettingsModel.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                    {
                        _logger.LogInformation($"Reconnecting to {clientConfig.Host} port {clientConfig.Port}");
                        await mqttClient.ConnectAsync(mqttClientOptionsBuilder.Build());
                    });
                });
            }

            Log.Debug($"Waiting {MqttSettingsModel.ConnectionDelayInMilliseconds} milliseconds before connecting to remote brokers");
            Task.Delay(MqttSettingsModel.ConnectionDelayInMilliseconds);

            // Connect to clients (all at once - in parallel)
            // Disconnect logic (above) will handle failures and retries
            Task.WaitAll(_mqttClients.Select(p => p.ConnectAsync(_mqttClientOptions[_mqttClients.IndexOf(p)])).Cast<Task>().ToArray());
        }

        public Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext httpContext)
        {
            return _webSocketServerAdapter.RunWebSocketConnectionAsync(webSocket, httpContext);
        }

        IMqttServerOptions CreateMqttServerOptions()
        {
            var options = new MqttServerOptionsBuilder()
                .WithMaxPendingMessagesPerClient(_settings.MaxPendingMessagesPerClient)
                .WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(_settings.CommunicationTimeout))
                .WithConnectionValidator(_mqttConnectionValidator)
                .WithApplicationMessageInterceptor(_mqttApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(_mqttSubscriptionInterceptor)
                .WithUnsubscriptionInterceptor(_mqttUnsubscriptionInterceptor)
                .WithClientId(_settings.BrokerClientId);

            // Configure unencrypted connections
            if (_settings.TcpEndPoint.Enabled)
            {
                options.WithDefaultEndpoint();

                if (_settings.TcpEndPoint.TryReadIPv4(out var address4))
                {
                    options.WithDefaultEndpointBoundIPAddress(address4);
                }

                if (_settings.TcpEndPoint.TryReadIPv6(out var address6))
                {
                    options.WithDefaultEndpointBoundIPV6Address(address6);
                }

                if (_settings.TcpEndPoint.Port > 0)
                {
                    options.WithDefaultEndpointPort(_settings.TcpEndPoint.Port);
                }
            }
            else
            {
                options.WithoutDefaultEndpoint();
            }

            // Configure encrypted connections
            if (_settings.EncryptedTcpEndPoint.Enabled)
            {
                options
                    .WithEncryptedEndpoint()
                    .WithEncryptionSslProtocol(SslProtocols.Tls13);

                if (!string.IsNullOrEmpty(_settings.EncryptedTcpEndPoint?.Certificate?.Path))
                {
                    IMqttServerCertificateCredentials certificateCredentials = null;

                    if (!string.IsNullOrEmpty(_settings.EncryptedTcpEndPoint?.Certificate?.Password))
                    {
                        certificateCredentials = new MqttServerCertificateCredentials
                        {
                            Password = _settings.EncryptedTcpEndPoint.Certificate.Password
                        };
                    }

                    options.WithEncryptionCertificate(_settings.EncryptedTcpEndPoint.Certificate.ReadCertificate(), certificateCredentials);
                }

                if (_settings.EncryptedTcpEndPoint.TryReadIPv4(out var address4))
                {
                    options.WithEncryptedEndpointBoundIPAddress(address4);
                }

                if (_settings.EncryptedTcpEndPoint.TryReadIPv6(out var address6))
                {
                    options.WithEncryptedEndpointBoundIPV6Address(address6);
                }

                if (_settings.EncryptedTcpEndPoint.Port > 0)
                {
                    options.WithEncryptedEndpointPort(_settings.EncryptedTcpEndPoint.Port);
                }
            }
            else
            {
                options.WithoutEncryptedEndpoint();
            }

            if (_settings.ConnectionBacklog > 0)
            {
                options.WithConnectionBacklog(_settings.ConnectionBacklog);
            }

            if (_settings.EnablePersistentSessions)
            {
                options.WithPersistentSessions();
            }

            return options.Build();
        }

    }
}