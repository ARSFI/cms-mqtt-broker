using System.Collections.Generic;

namespace MirroringMqttBroker.Configuration
{
    /// <summary>
    /// MQTT settings Model
    /// </summary>
    public class MqttSettingsModel
    {
        public const int ConnectionDelayInMilliseconds = 5000;

        public MqttSettingsModel()
        {
            RemoteBrokers = new List<RemoteBrokerConfiguration>();
            ClientCredentials = new List<ClientCredential>();
        }

        /// <summary>
        /// Set default connection timeout in seconds
        /// </summary>
        public int CommunicationTimeout { get; set; } = 15;

        /// <summary>
        /// Set 0 to disable connection backlogging
        /// </summary>
        public int ConnectionBacklog { get; set; }

        /// <summary>
        /// Enable support for persistent sessions
        /// </summary>
        public bool EnablePersistentSessions { get; set; } = false;

        /// <summary>
        /// Listen Settings
        /// </summary>
        public TcpEndPointModel TcpEndPoint { get; set; } = new TcpEndPointModel();

        /// <summary>
        /// Encryption Listen Settings
        /// </summary>
        public TcpEndPointModel EncryptedTcpEndPoint { get; set; } = new TcpEndPointModel();

        /// <summary>
        /// Settings for the Web Socket endpoint.
        /// </summary>
        public WebSocketEndPointModel WebSocketEndPoint { get; set; } = new WebSocketEndPointModel();

        /// <summary>
        /// Set limit for max pending messages per client
        /// </summary>
        public int MaxPendingMessagesPerClient { get; set; } = 250;

        /// <summary>
        /// Display name for the broker instance
        /// </summary>
        public string BrokerName { get; set; }

        /// <summary>
        /// Unique ID for this MQTT Broker instance
        /// </summary>
        public string BrokerClientId { get; set; }

        /// <summary>
        /// Flag to control use of client authentication 
        /// </summary>
        public bool RequireClientAuthentication { get; set; } = false;

        /// <summary>
        /// List of allowed client credentials. Used when RequireClientAuthentication is true.
        /// </summary>
        public List<ClientCredential> ClientCredentials { get; set; }

        /// <summary>
        /// The other broker(s) IP address/hostname, etc.
        /// </summary>
        public List<RemoteBrokerConfiguration> RemoteBrokers { get; set; }
    }
}