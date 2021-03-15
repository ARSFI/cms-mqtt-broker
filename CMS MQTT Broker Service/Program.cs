using Microsoft.Extensions.Configuration;
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
                IConfigurationRoot configurationRoot = config.Build();
                var connString = configurationRoot.GetValue<string>("sqlDatabaseConnectionString");
                context.Properties["cmsDatabase"] = new CMSDatabase(connectionString: connString);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MirroringMqttBroker>((serviceProvider) =>
                {
                    var cmsDatabase = hostContext.Properties["cmsDatabase"] as CMSDatabase;
                    var cmsProperties = new CMSProperties(cmsDatabase);
                    var serviceConfiguration = new DatabaseServiceConfiguration(cmsProperties);
                    return new MirroringMqttBroker(serviceConfiguration);
                });
            });
    }
}
