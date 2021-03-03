using System;
namespace winlink.cms.mqtt.config
{
    public interface IServiceConfiguration
    {
        // This broker's listening port.
        int ThisServicePort { get; }

        // The other broker's IP address/hostname.
        string OtherServiceHostname { get; }

        // The other broker's listening port.
        int OtherServicePort { get; }

        // Our client ID for communication with the other broker.
        string ThisClientId { get; }

        // Delay in milliseconds before attempting contact with other broker.
        int ConnectionDelayInMilliseconds { get; }

        // Delay in milliseconds before stopping contact with other broker.
        int StoppingDelayInMilliseconds { get; }
    }
}
