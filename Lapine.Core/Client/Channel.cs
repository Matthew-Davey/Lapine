namespace Lapine.Client {
    using System;
    using System.Collections.Generic;
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

        public async ValueTask DeleteExchangeAsync(String exchange, DeleteExchangeCondition condition = DeleteExchangeCondition.None) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeleteExchange(exchange, condition, promise));
            await promise.Task;
        }

        public async ValueTask DeclareQueueAsync(QueueDefinition definition) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeclareQueue(definition, promise));
            await promise.Task;
        }

        public async ValueTask DeleteQueueAsync(String queue, DeleteQueueCondition condition = DeleteQueueCondition.None) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new DeleteQueue(queue, condition, promise));
            await promise.Task;
        }

        public async ValueTask BindQueueAsync(String exchange, String queue, String routingKey = "#", IReadOnlyDictionary<String, Object>? arguments = null) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new BindQueue(exchange, queue, routingKey, arguments ?? new Dictionary<String, Object>(), promise));
            await promise.Task;
        }

        public async ValueTask PurgeQueueAsync(String queue) {
            var promise = new TaskCompletionSource();
            _system.Root.Send(_agent, new PurgeQueue(queue, promise));
            await promise.Task;
        }
    }
}
