AgentHierarchy
==============

```
root
└── amqp-client
    ├── socket*
    └── channel-manager
        ├── channel (0)*
        │   ├── frame-handler*
        │   ├── handshake*
        │   └── heartbeat
        ├── channel (1)*
        │   ├── frame-handler*
        │   └── publisher
        ├── channel (2)*
        │   ├── frame-handler*
        │   └── subscription
        │       └── assembler
        └── channel (3)*
            ├── frame-handler*
            └── subscription
                └── assembler
```

Message passing occurs only between parent-child agents, never between siblings.

If a message needs to be passed to a sibling, it should walk up the hierarchy to the nearest common ancestor, and then be routed back down.

\* = in progress
