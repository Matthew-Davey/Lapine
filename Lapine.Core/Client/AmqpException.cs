namespace Lapine.Client {
    using System;

    public class AmqpException : ApplicationException {
        public AmqpException(String message) : base(message) {
            // Intentionally empty...
        }

        static internal AmqpException Create(UInt16 replyCode, String message) =>
            replyCode switch {
                // 311 content-too-large
                312 => new UnroutableException(message.Replace("NO_ROUTE", String.Empty)),
                // 313 no-consumers
                // 320 connection-forced
                // 402 invalid-path
                // 403 access-refused
                // 404 not-found
                // 405 resource-locked
                406 => new PreconditionFailedException(message.Replace("PRECONDITION_FAILED - ", String.Empty)),
                // 501 frame-error
                // 502 syntax-error
                // 503 command-invalid
                // 504 channel-error
                // 505 unexpected-frame
                // 506 resource-error
                // 530 not-allowed
                // 540 not-implemented
                // 541 internal-error
                _ => new AmqpException(message)
            };
    }

    sealed class UnroutableException : AmqpException {
        public UnroutableException(String message) : base(message) {
            // Intentionally empty...
        }
    }

    sealed class PreconditionFailedException : AmqpException {
        public PreconditionFailedException(String message) : base(message) {
            // Intentionally empty...
        }
    }
}
