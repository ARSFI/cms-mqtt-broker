using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                services.AddHostedService<MirroringMqttBroker>();
            });
    }
}
