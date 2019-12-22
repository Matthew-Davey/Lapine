namespace Lapine.Agents.Commands {
    public class SocketTransmit {
        public ISerializable Item { get; }

        public SocketTransmit(ISerializable item) =>
            Item = item;
    }
}
