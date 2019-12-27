namespace Lapine.Protocol.Commands {
    using System;

    public interface ICommand : ISerializable {
        (Byte ClassId, Byte MethodId) CommandId { get; }
    }
}
