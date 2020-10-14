namespace Lapine.Client {
    using System;

    public class PreconditionFailedException : AmqpException {
        public PreconditionFailedException(String message) : base(message) {
            // Intentionally empty...
        }
    }
}
