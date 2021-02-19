namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public sealed record ConnectionConfiguration(
        IEnumerable<IPEndPoint> Endpoints,
        IEndpointSelectionStrategy EndpointSelectionStrategy,
        UInt16 ConnectionTimeout,
        IAuthenticationStrategy AuthenticationStrategy,
        String Locale,
        PeerProperties PeerProperties,
        String VirtualHost,
        UInt16 HeartbeatFrequency,
        UInt32 MaximumFrameSize,
        UInt16 MaximumChannelCount
    ) {
        public const UInt16 DefaultPort = 5672;
        public static IEndpointSelectionStrategy DefaultEndpointSelectionStrategy => new RandomEndpointSelectionStrategy();
        public const UInt16 DefaultConnectionTimeout = 5000;
        public static IAuthenticationStrategy DefaultAuthenticationStrategy => new PlainAuthenticationStrategy();
        public const String DefaultLocale = "en_US";
        public const String DefaultVirtualHost = "/";
        public const UInt16 DefaultHeartbeatFrequency = 60;
        public const UInt32 DefaultMaximumFrameSize = 131072; // From https://github.com/rabbitmq/rabbitmq-server/blob/7af37e5bb8bc4a517a6ab26a6038bef6cfa946e7/priv/schema/rabbit.schema#L564
        public const UInt16 DefaultMaximumChannelCount = 2047;

        static public ConnectionConfiguration Default => new (
            Endpoints                : new [] { new IPEndPoint(IPAddress.Loopback, DefaultPort) },
            EndpointSelectionStrategy: DefaultEndpointSelectionStrategy,
            ConnectionTimeout        : DefaultConnectionTimeout,
            AuthenticationStrategy   : DefaultAuthenticationStrategy,
            Locale                   : DefaultLocale,
            PeerProperties           : PeerProperties.Default,
            VirtualHost              : DefaultVirtualHost,
            HeartbeatFrequency       : DefaultHeartbeatFrequency,
            MaximumFrameSize         : DefaultMaximumFrameSize,
            MaximumChannelCount      : DefaultMaximumChannelCount
        );

        internal IEnumerator<IPEndPoint> GetEndpointEnumerator() =>
            EndpointSelectionStrategy.GetConnectionSequence(Endpoints)
                .ToList()
                .GetEnumerator();
    }
}
