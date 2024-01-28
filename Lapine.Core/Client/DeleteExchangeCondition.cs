namespace Lapine.Client;

[Flags]
public enum DeleteExchangeCondition {
    /// <summary>
    /// The server will delete the exchange unconditionally.
    /// </summary>
    None = 0x00,
    /// <summary>
    /// The server will only delete the exchange if it has no queue bindings.
    /// </summary>
    Unused = 0x01
}
