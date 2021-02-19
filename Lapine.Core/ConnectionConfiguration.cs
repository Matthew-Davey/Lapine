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

        public ConnectionConfiguration(IEnumerable<IPEndPoint> endpoints, IEndpointSelectionStrategy? endpointSelectionStrategy = null, UInt16 connectionTimeout = DefaultConnectionTimeout, IAuthenticationStrategy? authenticationStrategy = null, String locale = DefaultLocale, PeerProperties? peerProperties = null, String virtualHost = DefaultVirtualHost, UInt16 heartbeatFrequency = DefaultHeartbeatFrequency, UInt32 maximumFrameSize = DefaultMaximumFrameSize, UInt16 maximumChannelCount = DefaultMaximumChannelCount) {
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

        static public ConnectionConfiguration Default => new (
            endpoints: new [] { new IPEndPoint(IPAddress.Loopback, DefaultPort) }
        );

        public ConnectionConfiguration WithEndpoints(params IPEndPoint[] endpoints) => new (
            endpoints                : endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithEndpointSelectionStrategy(IEndpointSelectionStrategy endpointSelectionStrategy) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: endpointSelectionStrategy ?? throw new ArgumentNullException(nameof(endpointSelectionStrategy)),
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithConnectionTimeout(UInt16 connectionTimeout) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : connectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithAuthenticationStrategy(IAuthenticationStrategy authenticationStrategy) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : authenticationStrategy ?? throw new ArgumentNullException(nameof(authenticationStrategy)),
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithLocale(String locale) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : locale ?? throw new ArgumentNullException(nameof(locale)),
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithPeerProperties(PeerProperties peerProperties) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : peerProperties ?? throw new ArgumentNullException(nameof(peerProperties)),
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithVirtualHost(String virtualHost) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : virtualHost ?? throw new ArgumentNullException(nameof(virtualHost)),
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithHeartbeatFrequency(UInt16 heartbeatFrequency) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : heartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithMaximumFrameSize(UInt32 maximumFrameSize) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : maximumFrameSize,
            maximumChannelCount      : MaximumChannelCount
        );

        public ConnectionConfiguration WithMaximumChannelCount(UInt16 maximumChannelCount) => new (
            endpoints                : Endpoints,
            endpointSelectionStrategy: EndpointSelectionStrategy,
            connectionTimeout        : ConnectionTimeout,
            authenticationStrategy   : AuthenticationStrategy,
            locale                   : Locale,
            peerProperties           : PeerProperties,
            virtualHost              : VirtualHost,
            heartbeatFrequency       : HeartbeatFrequency,
            maximumFrameSize         : MaximumFrameSize,
            maximumChannelCount      : maximumChannelCount
        );

        internal IEnumerator<IPEndPoint> GetEndpointEnumerator() =>
            EndpointSelectionStrategy.GetConnectionSequence(Endpoints)
                .ToList()
                .GetEnumerator();
    }
}
