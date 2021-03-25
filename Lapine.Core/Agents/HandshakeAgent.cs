namespace Lapine.Agents {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lapine.Agents.Middleware;
    using Lapine.Protocol;
    using Lapine.Protocol.Commands;
    using Proto;
    using Proto.Timers;

    using static System.Math;
    using static System.Text.Encoding;
    using static System.Threading.Tasks.Task;
    using static Lapine.Agents.HandshakeAgent.Protocol;

    static class HandshakeAgent {
        static public class Protocol {
            public record BeginHandshake(ConnectionConfiguration ConnectionConfiguration, PID Listener, PID Dispatcher);
            public record HandshakeFailed(Exception Reason);
            public record HandshakeCompleted(UInt16 MaxChannelCount, UInt32 MaxFrameSize, TimeSpan HeartbeatFrequency, IReadOnlyDictionary<String, Object> ServerProperties);

            internal record TimeoutExpired();
        }

        static public Props Create() =>
            Props.FromProducer(() => new Actor())
                .WithContextDecorator(LoggingContextDecorator.Create)
                .WithReceiverMiddleware(FramingMiddleware.UnwrapInboundMethodFrames());

        class Actor : IActor {
            readonly Behavior _behaviour;

            public Actor() =>
                _behaviour = new Behavior(Unstarted);

            public Task ReceiveAsync(IContext context) =>
                _behaviour.ReceiveAsync(context);

            Task Unstarted(IContext context) {
                switch (context.Message) {
                    case BeginHandshake handshake: {
                        context.Scheduler().SendOnce(handshake.ConnectionConfiguration.ConnectionTimeout, context.Self!, new TimeoutExpired());
                        context.Send(handshake.Dispatcher, ProtocolHeader.Default);
                        _behaviour.Become(AwaitConnectionStart(handshake.ConnectionConfiguration, handshake.Listener, handshake.Dispatcher));
                        break;
                    }
                }
                return CompletedTask;
            }

            Receive AwaitConnectionStart(ConnectionConfiguration connectionConfiguration, PID listener, PID dispatcher) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(listener, new HandshakeFailed(new TimeoutException()));
                            _behaviour.Become(Unstarted);
                            return CompletedTask;
                        }
                        case ConnectionStart message: {
                            if (!message.Mechanisms.Contains(connectionConfiguration.AuthenticationStrategy.Mechanism)) {
                                context.Send(listener, new HandshakeFailed(new Exception($"Requested authentication mechanism '{connectionConfiguration.AuthenticationStrategy.Mechanism}' is not supported by the broker. This broker supports {String.Join(", ", message.Mechanisms)}")));
                                _behaviour.Become(Unstarted);
                                return CompletedTask;
                            }

                            if (!message.Locales.Contains(connectionConfiguration.Locale)) {
                                context.Send(listener, new HandshakeFailed(new Exception($"Requested locale '{connectionConfiguration.Locale}' is not supported by the broker. This broker supports {String.Join(", ", message.Locales)}")));
                                _behaviour.Become(Unstarted);
                                return CompletedTask;
                            }

                            // TODO: Verify protocol version compatibility...

                            var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(0, Array.Empty<Byte>());

                            context.Send(dispatcher, new ConnectionStartOk(
                                peerProperties: connectionConfiguration.PeerProperties.ToDictionary(),
                                mechanism     : connectionConfiguration.AuthenticationStrategy.Mechanism,
                                response      : UTF8.GetString(authenticationResponse),
                                locale        : connectionConfiguration.Locale
                            ));
                            _behaviour.Become(AwaitConnectionSecureOrTune(connectionConfiguration, listener, dispatcher, message.ServerProperties, 0));
                            return CompletedTask;
                        }
                        default: return CompletedTask;
                    }
                };

            Receive AwaitConnectionSecureOrTune(ConnectionConfiguration connectionConfiguration, PID listener, PID dispatcher, IReadOnlyDictionary<String, Object> serverProperties, Byte authenticationStage) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(listener, new HandshakeFailed(new TimeoutException()));
                            _behaviour.Become(Unstarted);
                            return CompletedTask;
                        }
                        case ConnectionSecure message: {
                            var challenge = UTF8.GetBytes(message.Challenge);
                            var authenticationResponse = connectionConfiguration.AuthenticationStrategy.Respond(stage: ++authenticationStage, challenge: challenge);
                            context.Send(dispatcher, new ConnectionSecureOk(UTF8.GetString(authenticationResponse)));
                            _behaviour.Become(AwaitConnectionSecureOrTune(connectionConfiguration, listener, dispatcher, serverProperties, authenticationStage));
                            return CompletedTask;
                        }
                        case ConnectionTune message: {
                            var heartbeatFrequency  = Min(message.Heartbeat, (UInt16)connectionConfiguration.HeartbeatFrequency.TotalSeconds);
                            var maximumFrameSize    = Min(message.FrameMax, connectionConfiguration.MaximumFrameSize);
                            var maximumChannelCount = Min(message.ChannelMax, connectionConfiguration.MaximumChannelCount);

                            context.Send(dispatcher, new ConnectionTuneOk(
                                channelMax: maximumChannelCount,
                                frameMax  : maximumFrameSize,
                                heartbeat : heartbeatFrequency
                            ));
                            context.Send(dispatcher, new ConnectionOpen(
                                virtualHost: connectionConfiguration.VirtualHost
                            ));
                            _behaviour.Become(AwaitConnectionOpenOk(listener, maximumChannelCount, maximumFrameSize, TimeSpan.FromSeconds(heartbeatFrequency), serverProperties));
                            return CompletedTask;
                        }
                        default: return CompletedTask;
                    }
                };

            Receive AwaitConnectionOpenOk(PID listener, UInt16 maxChannelCount, UInt32 maximumFrameSize, TimeSpan heartbeatFrequency, IReadOnlyDictionary<String, Object> serverProperties) =>
                (IContext context) => {
                    switch (context.Message) {
                        case TimeoutExpired _: {
                            context.Send(listener, new HandshakeFailed(new TimeoutException()));
                            break;
                        }
                        case ConnectionOpenOk _: {
                            context.Send(listener, new HandshakeCompleted(maxChannelCount, maximumFrameSize, heartbeatFrequency, serverProperties));
                            break;
                        }
                    }
                    _behaviour.Become(Unstarted);
                    return CompletedTask;
                };
        }
    }
}
