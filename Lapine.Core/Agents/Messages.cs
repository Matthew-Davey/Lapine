namespace Lapine.Agents {
    using System;

    public static class Messages {
        public const String Outbound = ":outbound"; // An atom/symbol denoting an outbound entity (sent from peer to server)
        public const String Connect = ":connect";
        public const String AddChannel = ":add-channel";
        public const String StartHeartbeatTransmission = ":start-heartbeat-transmission";
        public const String Inbound = ":inbound"; // An atom/symbol denoting an inbound entity (sent from server to peer)
        public const String AuthenticationFailed = ":authentication-failed";
        public const String HandshakeCompleted = ":handshake-completed";
        public const String HandshakeFailed = ":handshake-failed";
        public const String SocketConnected = ":socket-connected";
        public const String ConnectionReady = ":connection-ready";
        public const String ConnectionFailed = ":connection-failed";
        public const String Timeout = ":timeout";
    }
}
