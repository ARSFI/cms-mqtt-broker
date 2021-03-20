using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mirroring.mqtt.broker;
using mirroring.mqtt.broker.config;

namespace winlink.cms.mqtt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureAppConfiguration((context, config) =>
            {
                IConfigurationRoot configurationRoot = config.Build();
                var serviceConfiguration = new BrokerConfiguration();
                configurationRoot.Bind("BrokerConfiguration", serviceConfiguration);
                context.Properties["serviceConfiguration"] = serviceConfiguration;
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MirroringMqttBroker>((serviceProvider) =>
                {
                    var serviceConfiguration = hostContext.Properties["serviceConfiguration"] as BrokerConfiguration;
                    return new MirroringMqttBroker(serviceConfiguration);
                });
            });
    }

}
