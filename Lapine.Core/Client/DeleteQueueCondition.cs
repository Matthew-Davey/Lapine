namespace Lapine.Client;

[Flags]
public enum DeleteQueueCondition {
    /// <summary>
    /// The server will delete the queue unconditionally.
    /// </summary>
    None = 0x00,
    /// <summary>
    /// The server will only delete the queue if it has no consumers.
    /// </summary>
    Unused = 0x01,
    /// <summary>
    /// The server will only delete the queue if it has no messages.
    /// </summary>
    Empty = 0x02
}
