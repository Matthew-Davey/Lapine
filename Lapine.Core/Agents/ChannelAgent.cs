namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol.Commands;
    using Proto;

    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.ChannelAgent.Protocol;

    static class ChannelAgent {
        static public class Protocol {
            public record Open(PID Listener, UInt16 ChannelId, PID TxD);
            public record Opened(PID ChannelAgent);
            public record Close(TaskCompletionSource Promise);
            public record DeclareExchange(ExchangeDefinition Definition, TaskCompletionSource Promise);
            public record DeleteExchange(String Exchange, DeleteExchangeCondition Condition, TaskCompletionSource Promise);
            public record DeclareQueue(QueueDefinition Definition, TaskCompletionSource Promise);
            public record BindQueue(String Exchange, String Queue, String RoutingKey, IReadOnlyDictionary<String, Object> Arguments, TaskCompletionSource Promise);
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor())
                .WithContextDecorator(LoggingContextDecorator.Create)
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

        record State(PID CommandDispatcher);

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Closed);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Closed(IContext context) {
                switch (context.Message) {
                    case Open open: {
                        var state = new State(
                            CommandDispatcher: context.SpawnNamed(
                                name: "dispatcher",
                                props: DispatcherAgent.Create()
                            )
                        );
                        context.Send(state.CommandDispatcher, new DispatchTo(open.TxD, open.ChannelId));
                        context.Send(state.CommandDispatcher, new ChannelOpen());
                        _behaviour.Become(Opening(state, open.Listener));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive Opening(State state, PID listener) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ChannelOpenOk _: {
                            context.Send(listener, new Opened(context.Self!));
                            _behaviour.Become(Open(state));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive Open(State state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case Close close: {
                            context.Send(state.CommandDispatcher, new ChannelClose(0, String.Empty, (0, 0)));
                            _behaviour.Become(Closing(close.Promise));
                            break;
                        }
                        case DeclareExchange declare: {
                            context.Send(state.CommandDispatcher, new ExchangeDeclare(
                                exchangeName: declare.Definition.Name,
                                exchangeType: declare.Definition.Type,
                                passive     : false,
                                durable     : declare.Definition.Durability > Durability.Ephemeral,
                                autoDelete  : declare.Definition.AutoDelete,
                                @internal   : false,
                                noWait      : false,
                                arguments   : declare.Definition.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingExchangeDeclareOk(declare.Promise));
                            break;
                        }
                        case DeleteExchange delete: {
                            context.Send(state.CommandDispatcher, new ExchangeDelete(
                                exchangeName: delete.Exchange,
                                ifUnused    : delete.Condition.HasFlag(DeleteExchangeCondition.Unused),
                                noWait      : false
                            ));
                            _behaviour.BecomeStacked(AwaitingExchangeDeleteOk(delete.Promise));
                            break;
                        }
                        case DeclareQueue declare: {
                            context.Send(state.CommandDispatcher, new QueueDeclare(
                                queueName : declare.Definition.Name,
                                passive   : false,
                                durable   : declare.Definition.Durability > Durability.Ephemeral,
                                exclusive : declare.Definition.Exclusive,
                                autoDelete: declare.Definition.AutoDelete,
                                noWait    : false,
                                arguments : declare.Definition.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueDeclareOk(declare.Promise));
                            break;
                        }
                        case BindQueue bind: {
                            context.Send(state.CommandDispatcher, new QueueBind(
                                queueName   : bind.Queue,
                                exchangeName: bind.Exchange,
                                routingKey  : bind.RoutingKey,
                                noWait      : false,
                                arguments   : bind.Arguments
                            ));
                            _behaviour.BecomeStacked(AwaitingQueueBindOk(bind.Promise));
                            break;
                        }
                    }
                    return CompletedTask;
                };

            static Receive Closing(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ChannelCloseOk _: {
                            promise.SetResult();
                            context.Stop(context.Self!);
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingExchangeDeclareOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ExchangeDeclareOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingExchangeDeleteOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case ExchangeDeleteOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueDeclareOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueDeclareOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };

            Receive AwaitingQueueBindOk(TaskCompletionSource promise) =>
                (IContext context) => {
                    switch (context.Message) {
                        case QueueBindOk _: {
                            promise.SetResult();
                            _behaviour.UnbecomeStacked();
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
