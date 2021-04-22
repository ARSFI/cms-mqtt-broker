using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MirroringMqttBroker.Configuration;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MirroringMqttBroker.Mqtt
{
    public class MqttServerService : IMqttServerService
    {
        private readonly ILogger<MqttServerService> _logger;

        private readonly MqttSettingsModel _settings;
        private readonly MqttApplicationMessageInterceptor _mqttApplicationMessageInterceptor;
        private readonly MqttClientConnectedHandler _mqttClientConnectedHandler;
        private readonly MqttClientDisconnectedHandler _mqttClientDisconnectedHandler;
        private readonly MqttClientSubscribedTopicHandler _mqttClientSubscribedTopicHandler;
        private readonly MqttClientUnsubscribedTopicHandler _mqttClientUnsubscribedTopicHandler;
        private readonly MqttServerConnectionValidator _mqttConnectionValidator;
        private readonly MqttSubscriptionInterceptor _mqttSubscriptionInterceptor;
        private readonly MqttUnsubscriptionInterceptor _mqttUnsubscriptionInterceptor;
        private readonly MqttWebSocketServerAdapter _webSocketServerAdapter;

        private readonly IMqttServer _mqttServer;
        private List<IMqttClient> _mqttRemoteBrokers;
        private List<IMqttClientOptions> _mqttRemoteBrokerOptions;

        public IEnumerable<IMqttClient> RemoteBrokers => _mqttRemoteBrokers;

        public MqttSettingsModel Settings => _settings;

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

            // Make this object available to message interceptor class avoiding
            // circular references created when using dependency injection.
            _mqttApplicationMessageInterceptor.SetServerService(this);
        }

        public void Configure()
        {
            _mqttServer.ClientConnectedHandler = _mqttClientConnectedHandler;
            _mqttServer.ClientDisconnectedHandler = _mqttClientDisconnectedHandler;
            _mqttServer.ClientSubscribedTopicHandler = _mqttClientSubscribedTopicHandler;
            _mqttServer.ClientUnsubscribedTopicHandler = _mqttClientUnsubscribedTopicHandler;

            _mqttServer.StartAsync(CreateMqttServerOptions()).GetAwaiter().GetResult();
            _logger.LogInformation("MQTT server started.");

            // Connect to remote brokers 
            _mqttRemoteBrokers = new List<IMqttClient>();
            _mqttRemoteBrokerOptions = new List<IMqttClientOptions>();
            var mqttFactory = new MqttFactory();

            // Don't try to connect to other brokers if none defined.
            if (_settings.RemoteBrokers.Count > 0)
            {
                foreach (var remoteBrokerConfig in _settings.RemoteBrokers)
                {
                    var mqttRemoteBroker = mqttFactory.CreateMqttClient();
                    var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                        //prepend this broker's name to help identify the connection in logs
                        .WithClientId($"[From: {_settings.BrokerName}]{remoteBrokerConfig.ClientId}")
                        .WithCleanSession()
                        .WithCredentials(
                            remoteBrokerConfig.UserName,
                            remoteBrokerConfig.Password)
                        .WithTcpServer(
                            remoteBrokerConfig.Host,
                            remoteBrokerConfig.Port)
                        .WithProtocolVersion(MqttProtocolVersion.V500);
                    _mqttRemoteBrokers.Add(mqttRemoteBroker);
                    _mqttRemoteBrokerOptions.Add(mqttClientOptionsBuilder.Build());

                    mqttRemoteBroker.UseConnectedHandler((eventArgs) =>
                    {
                        _logger.LogInformation($"Connected to Remote broker '{remoteBrokerConfig.Name}' at: {remoteBrokerConfig.Host}:{remoteBrokerConfig.Port}");
                    });

                    // Sustain a disconnect and reconnect
                    mqttRemoteBroker.UseDisconnectedHandler(async (eventArgs) =>
                    {
                        await Task.Delay(MqttSettingsModel.ConnectionDelayInMilliseconds).ContinueWith(async (arg) =>
                        {
                            _logger.LogInformation($"Reconnecting to Remote broker '{remoteBrokerConfig.Name}' at: {remoteBrokerConfig.Host}:{remoteBrokerConfig.Port}");
                            await mqttRemoteBroker.ConnectAsync(mqttClientOptionsBuilder.Build());
                        });
                    });
                }
                Task.Delay(MqttSettingsModel.ConnectionDelayInMilliseconds).Wait();

                try
                {
                    // Connect to clients (all at once - in parallel)
                    Task.WaitAll(_mqttRemoteBrokers.Select(p => p.ConnectAsync(_mqttRemoteBrokerOptions[_mqttRemoteBrokers.IndexOf(p)])).Cast<Task>().ToArray());
                }
                catch (AggregateException ae)
                {
                    // Ignore initial errors
                    // Disconnect handler (above) will handle retries
                    ae.Handle(inner =>
                    {
                        _logger.LogTrace(ae.Message);
                        return true;
                    });
                }

            }
        }

        public Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext httpContext)
        {
            return _webSocketServerAdapter.RunWebSocketConnectionAsync(webSocket, httpContext);
        }

        IMqttServerOptions CreateMqttServerOptions()
        {
            // Create client id if none provided
            var cid = string.IsNullOrWhiteSpace(_settings.BrokerClientId) ? Guid.NewGuid().ToString("N").ToUpper() : _settings.BrokerClientId;

            var options = new MqttServerOptionsBuilder()
                .WithMaxPendingMessagesPerClient(_settings.MaxPendingMessagesPerClient)
                .WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(_settings.CommunicationTimeout))
                .WithConnectionValidator(_mqttConnectionValidator)
                .WithApplicationMessageInterceptor(_mqttApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(_mqttSubscriptionInterceptor)
                .WithUnsubscriptionInterceptor(_mqttUnsubscriptionInterceptor)
                .WithClientId(cid);

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
                    _logger.LogInformation($"MQTT Broker '{_settings.BrokerName}' listening on TCP port {_settings.TcpEndPoint.Port}, ClientID: {cid}");
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
                    _logger.LogInformation($"MQTT Broker {_settings.BrokerName} listening on SSL port {_settings.TcpEndPoint.Port}");
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