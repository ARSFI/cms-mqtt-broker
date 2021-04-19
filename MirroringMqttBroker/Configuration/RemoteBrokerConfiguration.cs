using System.Collections.Generic;

namespace MirroringMqttBroker.Configuration
{
    public class RemoteBrokerConfiguration
    {
        public RemoteBrokerConfiguration()
        {
            TopicFilters = new List<string>();
        }

        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        /// <summary>
        /// Unique ID for this remote broker client
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// User name used to authenticate with the remote broker (if required)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User Password used to authenticate with the remote broker (if required)
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// List of topics to forward to this remote broker
        /// </summary>
        public List<string> TopicFilters { get; set; }
    }
}
