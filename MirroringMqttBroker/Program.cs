using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace MirroringMqttBroker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Host.CreateDefaultBuilder(args)
                    .UseWindowsService()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(serverOptions =>
                            {
                            })
                            .ConfigureLogging(logging =>
                            {
                                logging.ClearProviders();
                                logging.SetMinimumLevel(LogLevel.Trace);
                            })
                            .UseUrls() // Remove default Kestrel ports
                            .UseNLog()
                            .UseStartup<Startup>();
                    })
                    .Build().Run();

                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return -1;
            }
        }
    }
}