using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mirroring.mqtt.broker;
using mirroring.mqtt.broker.config;
using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Extensions;

namespace winlink.cms.mqtt
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHostedMqttServer(mqttServer => mqttServer.WithoutDefaultEndpoint())
                .AddMqttConnectionHandler()
                .AddConnections();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapConnectionHandler<MqttConnectionHandler>(
                    "/mqtt",
                    httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                        protocolList =>
                            protocolList.FirstOrDefault() ?? string.Empty);
            });

            app.UseMqttServer(server =>
            {
                // Todo: Do something with the server
            });
        }
    }
}