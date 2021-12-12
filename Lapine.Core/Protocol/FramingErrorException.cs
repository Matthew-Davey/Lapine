namespace Lapine.Protocol;

class FramingErrorException : ProtocolErrorException {
    public FramingErrorException() : base() {
    }

    public FramingErrorException(String message, Exception? inner = null) : base(message, inner) {
    }
}
