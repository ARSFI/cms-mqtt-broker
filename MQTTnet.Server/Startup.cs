using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Server.Configuration;
using MQTTnet.Server.Logging;
using MQTTnet.Server.Mqtt;
using Newtonsoft.Json.Converters;

namespace MQTTnet.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void Configure(
            IApplicationBuilder application,
            MqttServerService mqttServerService,
            MqttSettingsModel mqttSettings)
        {
            application.UseDefaultFiles();
            application.UseStaticFiles();

            application.UseHsts();
            application.UseRouting();
            application.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            application.UseAuthentication();
            application.UseAuthorization();
     
            application.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            ConfigureWebSocketEndpoint(application, mqttServerService, mqttSettings);

            mqttServerService.Configure();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.AddControllers();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(o =>
                {
                    o.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            ReadMqttSettings(services);

            services.AddSingleton<MqttNetLoggerWrapper>();
            services.AddSingleton<CustomMqttFactory>();
            services.AddSingleton<MqttServerService>();
            services.AddSingleton<MqttServerStorage>();

            services.AddSingleton<MqttClientConnectedHandler>();
            services.AddSingleton<MqttClientDisconnectedHandler>();
            services.AddSingleton<MqttClientSubscribedTopicHandler>();
            services.AddSingleton<MqttClientUnsubscribedTopicHandler>();
            services.AddSingleton<MqttServerConnectionValidator>();
            services.AddSingleton<MqttSubscriptionInterceptor>();
            services.AddSingleton<MqttUnsubscriptionInterceptor>();
            services.AddSingleton<MqttApplicationMessageInterceptor>();

            services.AddAuthentication("Basic")
                .AddCookie();
        }

        void ReadMqttSettings(IServiceCollection services)
        {
            var mqttSettings = new MqttSettingsModel();
            Configuration.Bind("MQTT", mqttSettings);
            services.AddSingleton(mqttSettings);
        }

        static void ConfigureWebSocketEndpoint(
            IApplicationBuilder application,
            MqttServerService mqttServerService,
            MqttSettingsModel mqttSettings)
        {
            if (mqttSettings?.WebSocketEndPoint?.Enabled != true)
            {
                return;
            }

            if (string.IsNullOrEmpty(mqttSettings.WebSocketEndPoint.Path))
            {
                return;
            }

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(mqttSettings.WebSocketEndPoint.KeepAliveInterval)
            };

            application.UseWebSockets(webSocketOptions);

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == mqttSettings.WebSocketEndPoint.Path)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        string subProtocol = null;
                        if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                        {
                            subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                        }

                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false))
                        {
                            await mqttServerService.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    await next().ConfigureAwait(false);
                }
            });
        }
    }
}