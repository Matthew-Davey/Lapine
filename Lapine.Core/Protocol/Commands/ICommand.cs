namespace Lapine.Protocol.Commands;

interface ICommand : ISerializable {
    (Byte ClassId, Byte MethodId) CommandId { get; }
}
