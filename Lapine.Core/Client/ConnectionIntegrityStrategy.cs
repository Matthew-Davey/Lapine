namespace Lapine.Client;

using System;

public readonly record struct ConnectionIntegrityStrategy(TimeSpan? HeartbeatFrequency, (TimeSpan ProbeTime, TimeSpan RetryInterval, Int32 RetryCount)? KeepAliveSettings) {
    static public ConnectionIntegrityStrategy None => new (
        HeartbeatFrequency: null,
        KeepAliveSettings : null
    );

    static public ConnectionIntegrityStrategy Default => new (
        HeartbeatFrequency: TimeSpan.FromSeconds(60),
        KeepAliveSettings : null
    );

    static public ConnectionIntegrityStrategy AmqpHeartbeats(TimeSpan frequency) => new (
        HeartbeatFrequency: frequency,
        KeepAliveSettings : null
    );

    static public ConnectionIntegrityStrategy TcpKeepAlives(TimeSpan probeTime, TimeSpan retryInterval, Int32 retryCount) => new (
        HeartbeatFrequency: null,
        KeepAliveSettings : (probeTime, retryInterval, retryCount)
    );
}
