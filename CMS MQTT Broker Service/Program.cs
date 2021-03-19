using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using winlink.cms.mqtt.config;

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
                var serviceConfiguration = new ServiceConfiguration();
                configurationRoot.Bind("BrokerConfiguration", serviceConfiguration);
                context.Properties["serviceConfiguration"] = serviceConfiguration;
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MirroringMqttBroker>((serviceProvider) =>
                {
                    var serviceConfiguration = hostContext.Properties["serviceConfiguration"] as ServiceConfiguration;
                    return new MirroringMqttBroker(serviceConfiguration);
                });
            });
    }
}
