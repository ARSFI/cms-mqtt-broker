# CMS MQTT Broker

A multi-server cluster (2 initially) of MQTT brokers to enable sharing information between Winlink services.  

# Requirements
  MQTT broker /w support for simultaneous and multiple TCP and WebSocket connections
    Configurable interface and ports
  For use on Windows Server 2019 (CMS servers)
    Must run as windows service
  Relay messages received to the other broker [two initially]
    Configurable relay destination(s)
  Configuration stored in MySQL database (no local config files)

# Notes
  Possibly use MQTTNet library
  Existing CMS Service Manager program will control using System.ServiceProcess 

# Desired features
  Suport SSL/TLS connections 





