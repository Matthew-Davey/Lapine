namespace Lapine.Agents.Commands {
    using System;
    using Lapine.Protocol.Commands;

    public class TransmitCommand {
        public ICommand Command { get; }

        public TransmitCommand(ICommand command) =>
            Command = command ?? throw new ArgumentNullException(nameof(command));
    }
}
