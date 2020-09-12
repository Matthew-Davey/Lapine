namespace Lapine.Client {
    using System;
    using System.Threading.Tasks;
    using Lapine.Agents;
    using Lapine.Agents.Middleware;
    using Proto;
    using Proto.Schedulers.SimpleScheduler;

    using static Proto.Actor;

    public class AmqpClient : IAsyncDisposable {
        readonly ActorSystem _system;
        readonly ISimpleScheduler _scheduler;
        readonly ConnectionConfiguration _connectionConfiguration;
        readonly PID _agent;

        public AmqpClient(ConnectionConfiguration connectionConfiguration) {
            _system = new ActorSystem();
            _scheduler = new SimpleScheduler(_system.Root);
            _connectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
            _agent = _system.Root.SpawnNamed(
                name: "amqp-client",
                props: Props.FromProducer(() => new AmqpClientAgent(_connectionConfiguration))
                    .WithContextDecorator(LoggingContextDecorator.Create)
                    .WithChildSupervisorStrategy(new AllForOneStrategy((pid, reason) => SupervisorDirective.Stop, 1, TimeSpan.FromSeconds(1)))
            );
        }

        public Task ConnectAsync() {
            var onReady = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);

            _system.Root.SpawnNamed(
                name: "cmd-connect",
                props: Props.FromFunc(context => {
                    switch (context.Message) {
                        case Started _: {
                            _scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_connectionConfiguration.ConnectionTimeout), context.Self, (":timeout"));
                            context.Send(_agent, (":connect", notify: context.Self));
                            break;
                        }
                        case (":connection-ready"): {
                            onReady.SetResult(true);
                            context.Stop(context.Self);
                            break;
                        }
                        case (":connection-failed"): {
                            onReady.SetException(new Exception());
                            context.Stop(context.Self);
                            break;
                        }
                        case (":timeout"): {
                            onReady.SetException(new TimeoutException());
                            context.Stop(context.Self);
                            break;
                        }
                    }
                    return Done;
                })
                .WithContextDecorator(LoggingContextDecorator.Create)
            );

            return onReady.Task;
        }

        public Task<Channel> OpenChannel() {
            var onOpen = new TaskCompletionSource<Channel>(TaskCreationOptions.RunContinuationsAsynchronously);

            _system.Root.SpawnNamed(
                name: "cmd-open-channel",
                props: Props.FromFunc(context => {
                    switch (context.Message) {
                        case Started _: {
                            context.Send(_agent, (":open-channel", context.Self));
                            break;
                        }
                        case (":channel-opened", PID channel): {
                            onOpen.SetResult(new Channel(channel));
                            context.Stop(context.Self);
                            break;
                        }
                    }
                    return Done;
                })
            );

            return onOpen.Task;
        }

        public async ValueTask DisposeAsync() =>
            await _system.Root.StopAsync(_agent);
    }
}
