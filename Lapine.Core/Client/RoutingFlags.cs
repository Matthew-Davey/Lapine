namespace Lapine.Client;

using System;

[Flags]
public enum RoutingFlags {
    None = 0x00,
    Mandatory = 0x01,
    Immediate = 0x02
}
