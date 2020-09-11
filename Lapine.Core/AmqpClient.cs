namespace Lapine {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents;
    using Lapine.Agents.Middleware;
    using Proto;
    using Proto.Schedulers.SimpleScheduler;

    using static Proto.Actor;

    public class AmqpClient : IDisposable {
        readonly RootContext _context;
        readonly ISimpleScheduler _scheduler;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly PID _agent;

        public AmqpClient(ConnectionConfiguration connectionConfiguration) {
            _context                 = new RootContext();
            _scheduler               = new SimpleScheduler(_context);
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _agent = _context.SpawnNamed(
                name: "amqp-client",
                props: Props.FromProducer(() => new AmqpClientAgent(_connectionConfiguration))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithChildSupervisorStrategy(new AllForOneStrategy((pid, reason) => SupervisorDirective.Stop, 1, TimeSpan.FromSeconds(1)))
            );
        }

        public Task ConnectAsync() {
            var onReady = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);

            _context.SpawnNamed(
                name: "cmd-connect",
                props: Props.FromFunc(context => {
                    switch (context.Message) {
                        case Started _: {
                            _scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_connectionConfiguration.ConnectionTimeout), context.Self, (":timeout"));
                            _context.Send(_agent, (":connect", notify: context.Self));
                            break;
                        }
                        case (":connection-ready"): {
                            onReady.SetResult(true);
                            context.Self.Stop();
                            break;
                        }
                        case (":connection-failed"): {
                            onReady.SetException(new Exception());
                            context.Self.Stop();
                            break;
                        }
                        case (":timeout"): {
                            onReady.SetException(new TimeoutException());
                            context.Self.Stop();
                            break;
                        }
                    }
                    return Done;
                })
                .WithContextDecorator(LoggingContextDecorator.Create)
            );

            return onReady.Task;
        }

        public void Dispose() =>
            _agent.Stop();
    }
}
