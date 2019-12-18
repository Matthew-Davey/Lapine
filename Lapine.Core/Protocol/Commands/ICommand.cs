namespace Lapine.Protocol.Commands {
    using System;

    public interface ICommand {
        (Byte ClassId, Byte MethodId) CommandId { get; }
    }
}
