# CMS MQTT Broker

A multi-server cluster of MQTT brokers to enable sharing information between Winlink services.  

# Requirements
* MQTT broker /w support for simultaneous and multiple TCP and WebSocket connections
* Runs as windows service
* Relay messages received to the other broker(s)
* Configurable forwarding destination(s)
* Capable of filtering messages forwarded to other MQTT brokers
  * Use subscribe format as part remote broker configuration (multiples)
  
# Notes
* Uses MQTTNet library

# Desired features
* Suport SSL/TLS connections 






