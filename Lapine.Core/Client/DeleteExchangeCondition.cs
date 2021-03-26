namespace Lapine.Client {
    using System;

    [Flags]
    public enum DeleteExchangeCondition {
        None = 0x00,
        /// <summary>
        /// The server will only delete the exchange if it has no queue bindings.
        /// </summary>
        Unused = 0x01
    }
}
