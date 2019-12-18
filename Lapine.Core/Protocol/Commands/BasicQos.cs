namespace Lapine.Protocol.Commands {
    using System;

    public sealed class BasicQos : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0A);

        public UInt32 PrefetchSize { get; }
        public UInt16 PrefetchCount { get; }
        public Boolean Global { get; }

        public BasicQos(UInt32 prefetchSize, UInt16 prefetchCount, Boolean global) {
            PrefetchSize  = prefetchSize;
            PrefetchCount = prefetchCount;
            Global        = global;
        }
    }

    public sealed class BasicQosOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x3C, 0x0B);
    }
}
