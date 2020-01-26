namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class ConnectionConfiguration {
        public static UInt16 DefaultPort = 5672;
        public static IEndpointSelectionStrategy DefaultEndpointSelectionStrategy = new RandomEndpointSelectionStrategy();
        public const UInt16 DefaultConnectionTimeout = 5000;
        public static IAuthenticationStrategy DefaultAuthenticationStrategy = new PlainAuthenticationStrategy();
        public const String DefaultLocale = "en_US";
        public const String DefaultVirtualHost = "/";
        public const UInt16 DefaultHeartbeatFrequency = 60;
        public const UInt32 DefaultMaximumFrameSize = 131072; // From https://github.com/rabbitmq/rabbitmq-server/blob/7af37e5bb8bc4a517a6ab26a6038bef6cfa946e7/priv/schema/rabbit.schema#L564
        public const UInt16 DefaultMaximumChannelCount = 2047;

        public IEnumerable<IPEndPoint> Endpoints { get; }
        public IEndpointSelectionStrategy EndpointSelectionStrategy { get; }
        public UInt16 ConnectionTimeout { get; }
        public IAuthenticationStrategy AuthenticationStrategy { get; }
        public String Locale { get; }
        public PeerProperties PeerProperties { get; }
        public String VirtualHost { get; }
        public UInt16 HeartbeatFrequency { get; }
        public UInt32 MaximumFrameSize { get; }
        public UInt16 MaximumChannelCount { get; }

        public ConnectionConfiguration(IEnumerable<IPEndPoint> endpoints, IEndpointSelectionStrategy endpointSelectionStrategy = null, UInt16 connectionTimeout = DefaultConnectionTimeout, IAuthenticationStrategy authenticationStrategy = null, String locale = DefaultLocale, PeerProperties peerProperties = null, String virtualHost = DefaultVirtualHost, UInt16 heartbeatFrequency = DefaultHeartbeatFrequency, UInt32 maximumFrameSize = DefaultMaximumFrameSize, UInt16 maximumChannelCount = DefaultMaximumChannelCount) {
            Endpoints                 = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            EndpointSelectionStrategy = endpointSelectionStrategy ?? DefaultEndpointSelectionStrategy;
            ConnectionTimeout         = connectionTimeout;
            AuthenticationStrategy    = authenticationStrategy ?? DefaultAuthenticationStrategy;
            Locale                    = locale ?? throw new ArgumentNullException(nameof(locale));
            PeerProperties            = peerProperties ?? PeerProperties.Default;
            VirtualHost               = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));
            HeartbeatFrequency        = heartbeatFrequency;
            MaximumFrameSize          = maximumFrameSize;
            MaximumChannelCount       = maximumChannelCount;
        }

        static public ConnectionConfiguration Default => new ConnectionConfiguration(
            endpoints: new [] { new IPEndPoint(IPAddress.Loopback, DefaultPort) }
        );

        internal IEnumerator<IPEndPoint> GetEndpointEnumerator() =>
            EndpointSelectionStrategy.GetConnectionSequence(Endpoints)
                .ToList()
                .GetEnumerator();
    }
}
