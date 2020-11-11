namespace Lapine.Protocol {
    using System;

    class ProtocolErrorException : ApplicationException {
        public ProtocolErrorException() : base() {
        }

        public ProtocolErrorException(String message, Exception? inner = null) : base(message, inner) {
        }
    }
}
