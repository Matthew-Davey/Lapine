namespace Lapine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class ConnectionConfiguration {
        public IEnumerable<IPEndPoint> Endpoints { get; }
        public IEndpointSelectionStrategy EndpointSelectionStrategy { get; }

        public ConnectionConfiguration(IEnumerable<IPEndPoint> endpoints, IEndpointSelectionStrategy endpointSelectionStrategy) {
            Endpoints                 = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            EndpointSelectionStrategy = endpointSelectionStrategy ?? throw new ArgumentNullException(nameof(endpointSelectionStrategy));
        }

        internal IEnumerator<IPEndPoint> GetEndpointEnumerator() =>
            EndpointSelectionStrategy.GetConnectionSequence(Endpoints)
                .ToList()
                .GetEnumerator();
    }
}
