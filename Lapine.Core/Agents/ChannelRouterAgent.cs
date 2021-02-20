namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Protocol;
    using Proto;

    using static System.Threading.Tasks.Task;

    class ChannelRouterAgent : IActor {
        readonly IDictionary<UInt16, PID> _channels;

        public ChannelRouterAgent() =>
            _channels = new Dictionary<UInt16, PID>();

        public Task ReceiveAsync(IContext context) {
            switch (context.Message) {
                case (":add-channel", UInt16 channelId, PID channel): {
                    if (_channels.ContainsKey(channelId) == false) {
                        _channels.Add(channelId, channel);
                    }
                    return CompletedTask;
                }
                case (":receive", RawFrame frame): {
                    if (_channels.ContainsKey(frame.Channel)) {
                        context.Forward(_channels[frame.Channel]);
                    }
                    return CompletedTask;
                }
                case (":channel-closed", UInt16 channelNumber): {
                    if (_channels.ContainsKey(channelNumber)) {
                        _channels.Remove(channelNumber);
                    }
                    return CompletedTask;
                }
                default: return CompletedTask;
            }
        }
    }
}
