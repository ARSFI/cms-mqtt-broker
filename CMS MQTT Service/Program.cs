using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore.Extensions;

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
                .ConfigureLogging(logging =>
                {
                    // Remove default logging providers - just using NLog
                    // TODO: logging.ClearProviders();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(
                        o =>
                        {
                            o.ListenAnyIP(1883, l => l.UseMqtt()); // MQTT pipeline
                            o.ListenAnyIP(9001); // HTTP pipeline
                        });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
