AgentHierarchy
==============

```
root
└── amqp-client
    ├── socket*
    ├── channel-router*
    ├── channel (0)*
    │   ├── handshake*
    │   └── heartbeat*
    ├── channel (1)*
    │   └── publisher
    |       └── message-splitter
    ├── channel (2)*
    │   └── subscription
    |       ├── message-assembler
    │       └── consumer
    └── channel (3)*
        └── subscription
            ├── message-assembler
            └── consumer
```

Message passing occurs only between parent-child agents, never between siblings.

If a message needs to be passed to a sibling, it should walk up the hierarchy to the nearest common ancestor, and then be routed back down.

\* = in progress

## Agents

- **amqp-client** Root and entry point for the AMQP client, manages connection
- **socket** Encapsulates the TCP connection to the broker
- **channel-router** Routes frames to the necessary channel
- **channel** Encapsulates a channel, spawns child agents to perform specific tasks on the channel
- **handshake** Manages the connection handshake process (must only use channel zero)
- **heartbeat** Manages the connection heartbeat process (must only use channel zero)
- **publisher** Encapsulates message publishing, with or without publisher confirms etc
- **message-splitter** Splits messages into component header & content frames
- **subscription** Encapsulates a queue subscription, invoking consumers and acknowledging messages
- **message-assembler** Assembles messages out of component header and content frames
- **consumer** Executes external user-provided message handler code
