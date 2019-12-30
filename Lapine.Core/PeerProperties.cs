namespace Lapine {
    using System;

    using static System.Runtime.InteropServices.RuntimeInformation;

    public class PeerProperties {
        public String Product { get; }
        public String Version { get; }
        public String Platform { get; }
        public String Copyright { get; }
        public String Information { get; }

        public PeerProperties(String product, String version, String platform, String copyright, String information) {
            Product     = product;
            Version     = version;
            Platform    = platform;
            Copyright   = copyright;
            Information = information;
        }

        static public PeerProperties Default => new PeerProperties(
            product    : "Lapine",
            version    : "0.1.0",
            platform   : OSDescription,
            copyright  : "© Lapine Contributors 2019",
            information: String.Empty
        );
    }
}
