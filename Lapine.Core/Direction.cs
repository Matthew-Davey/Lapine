namespace Lapine {
    using System;

    public static class Direction {
        // An atom/symbol denoting an inbound entity (sent from server to peer)
        public const Byte Inbound = 0x00;

        // An atom/symbol denoting an outbound entity (sent from peer to server)
        public const Byte Outbound = 0x01;
    }
}
