namespace Lapine {
    using System;
    using System.Buffers;

    public interface ISerializable {
        IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer);
    }
}
