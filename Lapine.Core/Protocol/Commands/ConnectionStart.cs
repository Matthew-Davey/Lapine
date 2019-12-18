namespace Lapine.Protocol.Commands {
    using System;
    using System.Collections.Generic;

    public sealed class ConnectionStart : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0A);

        public (Byte Major, Byte Minor, Byte Revision) Version { get; }
        public IDictionary<String, Object> ServerProperties { get; }
        public IList<String> Mechanisms { get; }
        public IList<String> Locales { get; }

        public ConnectionStart((Byte Major, Byte Minor, Byte Revision) version, Byte minorVersion, Byte revision, IDictionary<String, Object> serverProperties, IList<String> mechanisms, IList<String> locales) {
            Version          = version;
            ServerProperties = serverProperties ?? throw new ArgumentNullException(nameof(serverProperties));
            Mechanisms       = mechanisms ?? throw new ArgumentNullException(nameof(mechanisms));
            Locales          = locales ?? throw new ArgumentNullException(nameof(locales));
        }
    }

    public sealed class ConnectionStartOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x0B);

        public IDictionary<String, Object> PeerProperties { get; }
        public String Mechanism { get; }
        public String Response { get; }
        public String Locale { get; }

        public ConnectionStartOk(IDictionary<String, Object> peerProperties, String mechanism, String response, String locale) {
            PeerProperties = peerProperties ?? throw new ArgumentNullException(nameof(peerProperties));
            Mechanism      = mechanism ?? throw new ArgumentNullException(nameof(mechanism));
            Response       = response ?? throw new ArgumentNullException(nameof(response));
            Locale         = locale ?? throw new ArgumentNullException(nameof(locale));
        }
    }
}
