using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Implementations;
using MQTTnet.Server.Configuration;

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

        public MqttServerService(
            MqttSettingsModel mqttSettings,
            CustomMqttFactory mqttFactory,
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

            _webSocketServerAdapter = new MqttWebSocketServerAdapter(mqttFactory.Logger);

            var adapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(mqttFactory.Logger)
                {
                    TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
                },
                _webSocketServerAdapter
            };

            _mqttServer = mqttFactory.CreateMqttServer(adapters);
        }

        public void Configure()
        {
            _mqttServer.ClientConnectedHandler = _mqttClientConnectedHandler;
            _mqttServer.ClientDisconnectedHandler = _mqttClientDisconnectedHandler;
            _mqttServer.ClientSubscribedTopicHandler = _mqttClientSubscribedTopicHandler;
            _mqttServer.ClientUnsubscribedTopicHandler = _mqttClientUnsubscribedTopicHandler;

            _mqttServer.StartAsync(CreateMqttServerOptions()).GetAwaiter().GetResult();

            _logger.LogInformation("MQTT server started.");
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
                .WithUnsubscriptionInterceptor(_mqttUnsubscriptionInterceptor);

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