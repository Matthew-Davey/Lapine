namespace Lapine.Protocol;

using Lapine.Protocol.Commands;

class ProtocolErrorException : ApplicationException {
    public ProtocolErrorException() {
    }

    public ProtocolErrorException(String message, Exception? inner = null) : base(message, inner) {
    }

    static internal ProtocolErrorException UnexpectedCommand(ICommand message) =>
        new ($"Received unexpected message from broker: {{{message}}}");
}
