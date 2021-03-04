using System;
using winlink.cms.data;

namespace winlink.cms.mqtt.config
{
    public class DatabaseServiceConfiguration : IServiceConfiguration
    {
        private CMSProperties propertiesTable;

        public DatabaseServiceConfiguration(CMSProperties properties)
        {
            propertiesTable = properties;
        }

        public int ThisServicePort => Convert.ToInt32(propertiesTable.GetProperty("mqttLocalServicePort", "1883"));

        public string OtherServiceHostname => propertiesTable.GetProperty("mqttRemoteServiceHostname", "cms-b");

        public int OtherServicePort => Convert.ToInt32(propertiesTable.GetProperty("mqttRemoteServicePort", "1883"));

        public string ThisClientId => propertiesTable.GetProperty("mqttClientId", "cms-a");

        public int ConnectionDelayInMilliseconds => Convert.ToInt32(propertiesTable.GetProperty("mqttConnectionDelayMilliseconds", "5000"));

        public int StoppingDelayInMilliseconds => Convert.ToInt32(propertiesTable.GetProperty("mqttStopDelayMilliseconds", "2000"));
    }
}
