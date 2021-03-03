using System;
using MySql.Data.MySqlClient;

namespace winlink.cms.mqtt.config
{
    public class MySQLServiceConfiguration : IServiceConfiguration
    {
        private MySqlConnection connection;

        public MySQLServiceConfiguration()
        {
            throw new NotImplementedException();
        }

        public int ThisServicePort => throw new NotImplementedException();

        public string OtherServiceHostname => throw new NotImplementedException();

        public int OtherServicePort => throw new NotImplementedException();

        public string ThisClientId => throw new NotImplementedException();

        public int ConnectionDelayInMilliseconds => throw new NotImplementedException();

        public int StoppingDelayInMilliseconds => throw new NotImplementedException();
    }
}
