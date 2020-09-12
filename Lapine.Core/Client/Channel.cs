namespace Lapine.Client {
    using System;
    using Proto;

    public class Channel {
        readonly PID _agent;

        internal Channel(PID agent) {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }
    }
}
