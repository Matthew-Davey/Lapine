namespace Lapine.Client;

public class ConsumerException : ApplicationException {
    public ConsumerException(String message)
        : base(message) {
    }

    public ConsumerException(String message, Exception inner)
        : base(message, inner) {
    }
}
