namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;

    using static Proto.Actor;

    public class ChannelRouterAgent : IActor {
        readonly IDictionary<UInt16, PID> _channels;

        public ChannelRouterAgent() =>
            _channels = new Dictionary<UInt16, PID>();

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case (":add-channel", UInt16 channelId, PID channel): {
                    if (_channels.ContainsKey(channelId) == false) {
                        _channels.Add(channelId, channel);
                    }
                    return Done;
                }
                case (":inbound", RawFrame frame): {
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
