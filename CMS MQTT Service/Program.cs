using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mirroring.mqtt.broker.config;
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
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging(logging =>
                {
                    // Remove default logging providers - just using NLog
                    // TODO: logging.ClearProviders();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("appSettings.json", optional: true);
                    //TODO: Not sure how to obtain configuration need for below port assignments
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(
                        o =>
                        {
                            //TODO: Wouldn't we need to route requests to our MirroringMqttBroker instance???
                            //TODO: Possibly by creating an extension similar to UseMqtt and using that below???

                            o.ListenAnyIP(1883, l => l.UseMqtt()); // MQTT pipeline
                            o.ListenAnyIP(9001); // HTTP pipeline
                        });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
