namespace Lapine.Client {
    using System;
    using System.Collections.Generic;

    using static System.Runtime.InteropServices.RuntimeInformation;

    public sealed record PeerProperties(
        String? Product,
        String? Version,
        String? Platform,
        String? Copyright,
        String? Information,
        String? ClientProvidedName
    ) {
        static public PeerProperties Default => new (
            Product           : "Lapine",
            Version           : "0.1.0",
            Platform          : OSDescription,
            Copyright         : "© Lapine Contributors 2019-2021",
            Information       : "Licensed under the MIT License https://opensource.org/licenses/MIT",
            ClientProvidedName: "Lapine 0.1.0"
        );

        static public PeerProperties Empty => new(
            Product           : null,
            Version           : null,
            Platform          : null,
            Copyright         : null,
            Information       : null,
            ClientProvidedName: null
        );

        public IReadOnlyDictionary<String, Object> ToDictionary() => new Dictionary<String, Object> {
            ["product"]         = Product ?? String.Empty,
            ["version"]         = Version ?? String.Empty,
            ["platform"]        = Platform ?? String.Empty,
            ["copyright"]       = Copyright ?? String.Empty,
            ["information"]     = Information ?? String.Empty,
            ["connection_name"] = ClientProvidedName ?? String.Empty
        };
    }
}
