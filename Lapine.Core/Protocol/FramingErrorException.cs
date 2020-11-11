namespace Lapine.Protocol {
    using System;

    class FramingErrorException : ApplicationException {
        public FramingErrorException() : base() {
        }

        public FramingErrorException(String message, Exception? inner = null) : base(message, inner) {
        }
    }
}
