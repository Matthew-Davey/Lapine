namespace Lapine {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents;
    using Lapine.Agents.Middleware;
    using Proto;

    public class AmqpClient : IDisposable {
        readonly RootContext _context;
        readonly ConnectionConfiguration _connectionConfiguration;
        PID _agent;

        public AmqpClient(ConnectionConfiguration connectionConfiguration) {
            _context = new RootContext();
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
        }

        public Task ConnectAsync() {
            _agent = _context.SpawnNamed(
                name: "amqp-client",
                props: Props.FromProducer(() => new AmqpClientAgent(_connectionConfiguration))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithChildSupervisorStrategy(new AllForOneStrategy((pid, reason) => SupervisorDirective.Stop, 1, TimeSpan.FromSeconds(1)))
            );

            var onReady = new TaskCompletionSource<Boolean>();

            _context.Send(_agent, (Messages.Connect, onReady));

            return onReady.Task;
        }

        public void Dispose() =>
            _agent.Stop();
    }
}
