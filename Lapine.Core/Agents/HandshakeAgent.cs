namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Client;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Timers;

    using static System.Math;
    using static System.Text.Encoding;
    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.DispatcherAgent.Protocol;
    using static Lapine.Agents.HandshakeAgent.Protocol;
    using static Lapine.Agents.SocketAgent.Protocol;

    using TimeoutExpired = Lapine.Agents.HandshakeAgent.Protocol.TimeoutExpired;

    static class HandshakeAgent {
        static public class Protocol {
            public record BeginHandshake(ConnectionConfiguration ConnectionConfiguration, PID Listener, PID Dispatcher);
            public record HandshakeFailed(Exception Reason);
            public record HandshakeCompleted(
                UInt16 MaxChannelCount,
                UInt32 MaxFrameSize,
                TimeSpan HeartbeatFrequency,
                IReadOnlyDictionary<String, Object> ServerProperties
            );

            internal record TimeoutExpired();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor())
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

        record NegotiatingState(
            Guid SubscriptionId,
            ConnectionConfiguration ConnectionConfiguration,
            PID Listener,
            PID Dispatcher,
            IReadOnlyDictionary<String, Object>? ServerProperties
        );

        record NegotiationCompleteState(
            Guid SubscriptionId,
            PID Listener,
            UInt16 MaxChannelCount,
            UInt32 MaxFrameSize,
            TimeSpan HeartbeatFrequency,
            IReadOnlyDictionary<String, Object> ServerProperties
        );

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Unstarted);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case BeginHandshake handshake: {
                        var subscription = context.System.EventStream.Subscribe<FrameReceived>(
                            predicate: message => message.Frame.Channel == 0,
                            action   : message => context.Send(context.Self!, message)
                        );
                        context.Scheduler().SendOnce(
                            delay  : handshake.ConnectionConfiguration.ConnectionTimeout,
                            target : context.Self!,
                            message: new TimeoutExpired()
                        );
                        context.Send(handshake.Dispatcher, Dispatch.ProtocolHeader(ProtocolHeader.Default));
                        _behaviour.Become(AwaitConnectionStart(new NegotiatingState(
                            SubscriptionId         : subscription.Id,
                            ConnectionConfiguration: handshake.ConnectionConfiguration,
                            Listener               : handshake.Listener,
                            Dispatcher             : handshake.Dispatcher,
                            ServerProperties       : null
                        )));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitConnectionStart(NegotiatingState state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(state.Listener, new HandshakeFailed(new TimeoutException()));
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            context.Stop(context.Self!);
                            return CompletedTask;
                        }
                        case ConnectionStart message when !message.Mechanisms.Contains(state.ConnectionConfiguration.AuthenticationStrategy.Mechanism): {
                            context.Send(state.Listener, new HandshakeFailed(new Exception($"Requested authentication mechanism '{state.ConnectionConfiguration.AuthenticationStrategy.Mechanism}' is not supported by the broker. This broker supports {String.Join(", ", message.Mechanisms)}")));
                            _behaviour.Become(Unstarted);
                            return CompletedTask;
                        }
                        case ConnectionStart message when !message.Locales.Contains(state.ConnectionConfiguration.Locale): {
                            context.Send(state.Listener, new HandshakeFailed(new Exception($"Requested locale '{state.ConnectionConfiguration.Locale}' is not supported by the broker. This broker supports {String.Join(", ", message.Locales)}")));
                            _behaviour.Become(Unstarted);
                            return CompletedTask;
                        }
                        case ConnectionStart message: {
                            // TODO: Verify protocol version compatibility...
                            var authenticationResponse = state.ConnectionConfiguration.AuthenticationStrategy.Respond(
                                stage    : 0,
                                challenge: Span<Byte>.Empty
                            );
                            context.Send(state.Dispatcher, Dispatch.Command(new ConnectionStartOk(
                                peerProperties: state.ConnectionConfiguration.PeerProperties.ToDictionary(),
                                mechanism     : state.ConnectionConfiguration.AuthenticationStrategy.Mechanism,
                                response      : UTF8.GetString(authenticationResponse),
                                locale        : state.ConnectionConfiguration.Locale
                            )));
                            _behaviour.Become(AwaitConnectionSecureOrTune(
                                authenticationStage: 0,
                                state              : state with {
                                    ServerProperties = message.ServerProperties
                                }
                            ));
                            return CompletedTask;
                        }
                        default: return CompletedTask;
                    }
                };

            Receive AwaitConnectionSecureOrTune(NegotiatingState state, Byte authenticationStage) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(state.Listener, new HandshakeFailed(new TimeoutException()));
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            context.Stop(context.Self!);
                            return CompletedTask;
                        }
                        case ConnectionSecure message: {
                            var challenge = UTF8.GetBytes(message.Challenge);
                            var authenticationResponse = state.ConnectionConfiguration.AuthenticationStrategy.Respond(
                                stage    : ++authenticationStage,
                                challenge: challenge
                            );
                            context.Send(state.Dispatcher, Dispatch.Command(new ConnectionSecureOk(
                                response: UTF8.GetString(authenticationResponse)
                            )));
                            _behaviour.Become(AwaitConnectionSecureOrTune(
                                state              : state,
                                authenticationStage: authenticationStage
                            ));
                            return CompletedTask;
                        }
                        case ConnectionTune tune: {
                            var heartbeatFrequency = Min(tune.Heartbeat, (UInt16)state.ConnectionConfiguration.ConnectionIntegrityStrategy.HeartbeatFrequency.GetValueOrDefault().TotalSeconds);
                            var maxFrameSize       = Min(tune.FrameMax, state.ConnectionConfiguration.MaximumFrameSize);
                            var maxChannelCount    = Min(tune.ChannelMax, state.ConnectionConfiguration.MaximumChannelCount);

                            context.Send(state.Dispatcher, Dispatch.Command(new ConnectionTuneOk(
                                channelMax: maxChannelCount,
                                frameMax  : maxFrameSize,
                                heartbeat : heartbeatFrequency
                            )));
                            context.Send(state.Dispatcher, Dispatch.Command(new ConnectionOpen(
                                virtualHost: state.ConnectionConfiguration.VirtualHost
                            )));
                            _behaviour.Become(AwaitConnectionOpenOk(new NegotiationCompleteState(
                                SubscriptionId    : state.SubscriptionId,
                                Listener          : state.Listener,
                                MaxChannelCount   : maxChannelCount,
                                MaxFrameSize      : maxFrameSize,
                                HeartbeatFrequency: TimeSpan.FromSeconds(heartbeatFrequency),
                                ServerProperties  : state.ServerProperties!
                            )));
                            return CompletedTask;
                        }
                        default: return CompletedTask;
                    }
                };

            static Receive AwaitConnectionOpenOk(NegotiationCompleteState state) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(state.Listener, new HandshakeFailed(new TimeoutException()));
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            context.Stop(context.Self!);
                            break;
                        }
                        case ConnectionOpenOk _: {
                            context.System.EventStream.Unsubscribe(state.SubscriptionId);
                            context.Send(state.Listener, new HandshakeCompleted(
                                MaxChannelCount   : state.MaxChannelCount,
                                MaxFrameSize      : state.MaxFrameSize,
                                HeartbeatFrequency: state.HeartbeatFrequency,
                                ServerProperties  : state.ServerProperties
                            ));
                            context.Stop(context.Self!);
                            break;
                        }
                    }
                    return CompletedTask;
                };
        }
    }
}
