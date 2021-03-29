using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mirroring.mqtt.broker;
using mirroring.mqtt.broker.config;
using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Server;
using NLog.Fluent;

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
                server.StartedHandler = new MqttServerStartedHandlerDelegate(async args =>
                {
                    Log.Debug($"Waiting {BrokerConfiguration.ConnectionDelayInMilliseconds} milliseconds before connecting to remote brokers");
                    await Task.Delay(BrokerConfiguration.ConnectionDelayInMilliseconds);

                    // Connect to clients (all at once - in parallel)
                    // Disconnect logic (above) will handle failures and retries
                    //Task.WaitAll(_mqttClients.Select(p => p.ConnectAsync(_mqttClientOptions[_mqttClients.IndexOf(p)])).Cast<Task>().ToArray());
                });

                server.StoppedHandler = new MqttServerStoppedHandlerDelegate(async args =>
                {
                    /*foreach (var unused in _serviceConfiguration.RemoteMqttBrokers)
                    {
                        await _mqttClients[index].DisconnectAsync();
                        index++;
                    }*/
                });
            });
        }
    }
}