namespace Lapine.Client;

public class MessageException : ApplicationException {
    public MessageException(String message)
        : base(message) {
    }

    public MessageException(String message, Exception inner)
        : base(message, inner) {
    }
}
