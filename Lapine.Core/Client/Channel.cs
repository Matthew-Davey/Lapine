namespace Lapine.Client {
    using System;
    using System.Threading.Tasks;
    using Proto;

    public class Channel {
        readonly ActorSystem _system;
        readonly PID _agent;

        internal Channel(ActorSystem system, PID agent) {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public Task Close() {
            var onClosed = new TaskCompletionSource<Boolean>();

            _system.Root.SpawnPrefix(
                prefix: "cmd-close-channel",
                props: Props.FromFunc(context => {
                    switch (context.Message) {
                        case Started _: {
                            context.Send(_agent, (":close", context.Self));
                            break;
                        }
                        case (":channel-closed", UInt16 _): {
                            onClosed.SetResult(true);
                            context.Stop(context.Self);
                            break;
                        }
                    }
                    return Actor.Done;
                })
            );

            return onClosed.Task;
        }

        public Task DeclareExchange(ExchangeDefinition definition) {
            var onDeclared = new TaskCompletionSource<Boolean>();

            _system.Root.SpawnPrefix(
                prefix: "cmd-exchange-declare",
                props: Props.FromFunc(context => {
                    switch (context.Message) {
                        case Started _: {
                            context.Send(_agent, (":exchange-declare", definition, context.Self));
                            break;
                        }
                        case (":exchange-declared"): {
                            onDeclared.SetResult(true);
                            context.Stop(context.Self);
                            break;
                        }
                        case (":exchange-declare-failed", UInt16 replyCode, String replyText): {
                            onDeclared.SetException(AmqpException.Create(replyCode, replyText));
                            context.Stop(context.Self);
                            break;
                        }
                    }
                    return Actor.Done;
                })
            );

            return onDeclared.Task;
        }
    }
}
