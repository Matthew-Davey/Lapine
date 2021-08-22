namespace Lapine.Protocol {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the outcome of a connection negotiation - the agreed connection properties.
    /// </summary>
    readonly record struct ConnectionAgreement(
        UInt16 MaxChannelCount,
        UInt32 MaxFrameSize,
        TimeSpan HeartbeatFrequency,
        IReadOnlyDictionary<String, Object> ServerProperties
    );
}
