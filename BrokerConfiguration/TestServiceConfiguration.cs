using System;
namespace winlink.cms.mqtt.config
{
    public class TestServiceConfiguration : IServiceConfiguration
    {
        public TestServiceConfiguration()
        {
        }

        public int ThisServicePort => 1883;

        public string OtherServiceHostname => "localhost"; // TBD

        public int OtherServicePort => 1883;

        public string ThisClientId => "cms-a";

        public int ConnectionDelayInMilliseconds => 5000;

        public int StoppingDelayInMilliseconds => 2000;
    }
}
