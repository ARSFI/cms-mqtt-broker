using System.Collections.Generic;

namespace MirroringMqttBroker.Configuration
{
    public class RemoteBrokerConfiguration
    {
        public RemoteBrokerConfiguration()
        {
            TopicFilters = new List<string>();
        }

        public string Host { get; set; }
        public int Port { get; set; }

        /// <summary>
        /// Unique ID for this remote broker client
        /// </summary>
        public string ClientId { get; set; }

        public string UserId { get; set; }
        public string UserPassword { get; set; }
        
        /// <summary>
        /// List of topics to forward to this remote broker
        /// </summary>
        public List<string> TopicFilters { get; set; }
    }
}
