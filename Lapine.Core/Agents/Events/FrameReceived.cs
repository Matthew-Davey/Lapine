namespace Lapine.Agents.Events {
    using System;
    using Lapine.Protocol;

    public class FrameReceived {
        public RawFrame Frame { get; }

        public FrameReceived(RawFrame frame) =>
            Frame = frame;
    }
}
