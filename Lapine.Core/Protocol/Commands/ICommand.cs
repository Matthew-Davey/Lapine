namespace Lapine.Protocol.Commands {
    using System;

    interface ICommand : ISerializable {
        (Byte ClassId, Byte MethodId) CommandId { get; }
    }
}
