using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using winlink.cms.data;
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
                // configure the app here.
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MirroringMqttBroker>((serviceProvider) =>
                {
                    // TBD: load config string from appsettings
                    var cmsDatabase = new CMSDatabase("testMySSQLConnectionString");
                    var cmsProperties = new CMSProperties(cmsDatabase);
                    var serviceConfiguration = new DatabaseServiceConfiguration(cmsProperties);
                    return new MirroringMqttBroker(serviceConfiguration);
                });
            });
    }
}
