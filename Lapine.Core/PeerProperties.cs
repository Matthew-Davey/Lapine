namespace Lapine {
    using System;
    using System.Collections.Generic;

    using static System.Runtime.InteropServices.RuntimeInformation;

    public class PeerProperties {
        public String Product { get; }
        public String Version { get; }
        public String Platform { get; }
        public String Copyright { get; }
        public String Information { get; }
        public String ClientProvidedName { get; } // Used by RabbitMQ Management Console to identify the connection

        public PeerProperties(String product, String version, String platform, String copyright, String information, String clientProvidedName = null) {
            Product            = product;
            Version            = version;
            Platform           = platform;
            Copyright          = copyright;
            Information        = information;
            ClientProvidedName = clientProvidedName ?? Product;
        }

        static public PeerProperties Default => new PeerProperties(
            product    : "Lapine",
            version    : "0.1.0",
            platform   : OSDescription,
            copyright  : "© Lapine Contributors 2019",
            information: "Licensed under the MIT License https://opensource.org/licenses/MIT"
        );

        public IReadOnlyDictionary<String, Object> ToDictionary() => new Dictionary<String, Object> {
            ["product"]         = Product,
            ["version"]         = Version,
            ["platform"]        = Platform,
            ["copyright"]       = Copyright,
            ["information"]     = Information,
            ["connection_name"] = ClientProvidedName
        };
    }
}
