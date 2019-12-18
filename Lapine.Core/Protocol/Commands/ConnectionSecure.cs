namespace Lapine.Protocol.Commands {
    using System;

    public sealed class ConnectionSecure : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x14);

        public String Challenge { get; }

        public ConnectionSecure(String challenge) =>
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
    }

    public sealed class ConnectionSecureOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x0A, 0x15);

        public String Response { get; }

        public ConnectionSecureOk(String response) =>
            Response = response ?? throw new ArgumentNullException(nameof(response));
    }
}
