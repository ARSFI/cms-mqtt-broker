using System;
namespace winlink.cms.mqtt.config
{
    public class ServiceConfigurationFactory
    {
        public ServiceConfigurationFactory()
        {
        }

        public IServiceConfiguration GetConfiguration()
        {
            return new TestServiceConfiguration();
        }
    }
}
