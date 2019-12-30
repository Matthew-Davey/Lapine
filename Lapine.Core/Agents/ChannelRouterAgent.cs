namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;

    using static Lapine.Direction;
    using static Proto.Actor;

    public class ChannelRouterAgent : IActor {
        readonly IDictionary<UInt16, PID> _channels;

        public ChannelRouterAgent() =>
            _channels = new Dictionary<UInt16, PID>();

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case (UInt16 channel, PID pid): {
                    if (_channels.ContainsKey(channel) == false) {
                        _channels.Add(channel, pid);
                    }
                    return Done;
                }
                case (Inbound, RawFrame frame): {
                    if (_channels.ContainsKey(frame.Channel)) {
                        context.Forward(_channels[frame.Channel]);
                    }
                    return Done;
                }
                default: return Done;
            }
        }
    }
}
