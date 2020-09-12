namespace Lapine {
    using System;
    using System.Buffers;

    interface ISerializable {
        IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer);
    }
}
