namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class ConnectionConfiguration {
        public IEnumerable<IPEndPoint> Endpoints { get; }
        public IEndpointSelectionStrategy EndpointSelectionStrategy { get; }
        public PeerProperties PeerProperties { get; }
        public String VirtualHost { get; }

        public const String DefaultVirtualHost = "/";

        public ConnectionConfiguration(IEnumerable<IPEndPoint> endpoints, IEndpointSelectionStrategy endpointSelectionStrategy, PeerProperties peerProperties = null, String virtualHost = null) {
            Endpoints                 = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            EndpointSelectionStrategy = endpointSelectionStrategy ?? throw new ArgumentNullException(nameof(endpointSelectionStrategy));
            PeerProperties            = peerProperties ?? PeerProperties.Default;
            VirtualHost               = virtualHost ?? DefaultVirtualHost;
        }

        internal IEnumerator<IPEndPoint> GetEndpointEnumerator() =>
            EndpointSelectionStrategy.GetConnectionSequence(Endpoints)
                .ToList()
                .GetEnumerator();
    }
}
