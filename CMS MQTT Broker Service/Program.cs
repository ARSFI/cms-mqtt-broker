using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mirroring.mqtt.broker;
using mirroring.mqtt.broker.config;
using NLog;

namespace winlink.cms.mqtt
{
    public class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            Log.Info($"Starting {GetAssemblyName()} - v {GetAssemblyVersion()}");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging(logging =>
            {
                // Remove default logging providers - just using NLog
                logging.ClearProviders();
            })
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

        private static string GetAssemblyName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        private static Version GetAssemblyVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version;
        }

    }

}
