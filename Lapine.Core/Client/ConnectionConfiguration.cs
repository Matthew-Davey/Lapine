namespace Lapine.Client {
    using System;
    using System.Linq;
    using System.Net;

    public sealed record ConnectionConfiguration(
        IPEndPoint[] Endpoints,
        IEndpointSelectionStrategy EndpointSelectionStrategy,
        TimeSpan ConnectionTimeout,
        TimeSpan CommandTimeout,
        IAuthenticationStrategy AuthenticationStrategy,
        String Locale,
        PeerProperties PeerProperties,
        String VirtualHost,
        ConnectionIntegrityStrategy ConnectionIntegrityStrategy,
        UInt32 MaximumFrameSize,
        UInt16 MaximumChannelCount
    ) {
        public const UInt16 DefaultPort = 5672;
        public static IEndpointSelectionStrategy DefaultEndpointSelectionStrategy =>
            new RandomEndpointSelectionStrategy();
        public static TimeSpan DefaultConnectionTimeout => TimeSpan.FromSeconds(5);
        public static TimeSpan DefaultCommandTimeout => TimeSpan.FromSeconds(5);
        public static IAuthenticationStrategy DefaultAuthenticationStrategy =>
            new PlainAuthenticationStrategy();
        public const String DefaultLocale = "en_US";
        public const String DefaultVirtualHost = "/";
        public static ConnectionIntegrityStrategy DefaultConnectionIntegrityStategy =>
            ConnectionIntegrityStrategy.Default;
        public const UInt32 DefaultMaximumFrameSize = 131072; // From https://github.com/rabbitmq/rabbitmq-server/blob/7af37e5bb8bc4a517a6ab26a6038bef6cfa946e7/priv/schema/rabbit.schema#L564
        public const UInt16 DefaultMaximumChannelCount = 2047;

        static public ConnectionConfiguration Default => new (
            Endpoints                  : new [] { new IPEndPoint(IPAddress.Loopback, DefaultPort) },
            EndpointSelectionStrategy  : DefaultEndpointSelectionStrategy,
            ConnectionTimeout          : DefaultConnectionTimeout,
            CommandTimeout             : DefaultCommandTimeout,
            AuthenticationStrategy     : DefaultAuthenticationStrategy,
            Locale                     : DefaultLocale,
            PeerProperties             : PeerProperties.Default,
            VirtualHost                : DefaultVirtualHost,
            ConnectionIntegrityStrategy: DefaultConnectionIntegrityStategy,
            MaximumFrameSize           : DefaultMaximumFrameSize,
            MaximumChannelCount        : DefaultMaximumChannelCount
        );

        internal IPEndPoint[] GetConnectionSequence() =>
            EndpointSelectionStrategy
                .GetConnectionSequence(Endpoints)
                .ToArray();
    }
}
