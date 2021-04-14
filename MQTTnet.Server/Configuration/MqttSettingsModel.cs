using System.Collections.Generic;

namespace MQTTnet.Server.Configuration
{
    /// <summary>
    /// MQTT settings Model
    /// </summary>
    public class MqttSettingsModel
    {
        public const int ConnectionDelayInMilliseconds = 5000;
        public const int StoppingDelayInMilliseconds = 2000;

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
        /// Flag to control use of client authentication 
        /// </summary>
        public bool RequireClientAuthentication { get; set; } = false;
        
        /// <summary>
        /// Unique ID for this MQTT Broker instance
        /// </summary>
        public string BrokerClientId { get; set; }

        /// <summary>
        /// The username to use to connect to this broker 
        /// </summary>
        public string BrokerUsername { get; set; }

        /// <summary>
        /// The password to use to connect to this broker
        /// </summary>
        public string BrokerPassword { get; set; }

        /// <summary>
        /// The other broker(s) IP address/hostname, etc.
        /// </summary>
        public List<RemoteBrokerConfiguration> RemoteBrokers { get; set; }
    }
}