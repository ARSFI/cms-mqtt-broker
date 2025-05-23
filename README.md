# Mirroring MQTT Broker

Mirroring MQTT Broker provides the ability to create a multi-server cluster of MQTT brokers to enable sharing information between broker instances.  

Based on example project from MQTTnet project: https://github.com/chkr1011/MQTTnet

# Features
* MQTT broker /w support for simultaneous and multiple TCP and WebSocket connections
* Runs as windows service
* Relays messages received to the other brokers (standard or mirroring)
* Protects against message loops
* Configurable forwarding destinations
* Support for client authentication
* Capable of filtering messages forwarded to other MQTT brokers
  * Uses standard MQTT topic filter format

# Configuration
* All configuration is made in the appSettings.json file. See comments in file for details.

The 'Kestrel' section of the appsSettings.json file is used to define the ports and protocols used for web socket connections

The 'MQTT' section contains the bulk of other settings.










