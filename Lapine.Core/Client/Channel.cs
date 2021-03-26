namespace Lapine.Client {
    using System;
    using System.Threading.Tasks;
    using Proto;

    using static Lapine.Agents.ChannelAgent.Protocol;

    public class Channel {
        readonly ActorSystem _system;
        readonly PID _agent;

        internal Channel(ActorSystem system, PID agent) {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public async ValueTask Close() {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new Close(promise));
            await promise.Task;
        }

        public async ValueTask DeclareExchangeAsync(ExchangeDefinition definition) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeclareExchange(definition, promise));
            await promise.Task;
        }

        public async ValueTask DeclareQueueAsync(QueueDefinition definition) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeclareQueue(definition, promise));
            await promise.Task;
        }
    }
}
