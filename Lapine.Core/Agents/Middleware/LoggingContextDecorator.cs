namespace Lapine.Agents.Middleware {
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Proto;

    public class LoggingContextDecorator : ActorContextDecorator
    {
        readonly ILogger _log;
        readonly LogLevel _level;

        public LoggingContextDecorator(IContext context, LogLevel level = LogLevel.Debug) : base(context) {
            _log   = Lapine.Log.CreateLogger(GetType());
            _level = level;
        }

        public override void Forward(PID target) {
            _log.Log(_level, "Agent {agent} forwarding `{messageType}` to {target}", Self.ToShortString(), Message.GetType(), target.ToShortString());
            base.Forward(target);
        }

        public override Task Receive(MessageEnvelope envelope) {
            _log.Log(_level, "Agent {agent} receiving `{messageType}`", Self.ToShortString(), envelope.Message.GetType());
            return base.Receive(envelope);
        }

        public override void Request(PID target, Object message) {
            _log.Log(_level, "Agent {agent} sending request message `{messageType}` to {target}", Self.ToShortString(), message.GetType(), target.ToShortString());
            base.Request(target, message);
        }

        public override void Respond(Object message) {
            _log.Log(_level, "Agent {agent} responding to request message with reply `{messageType}`", Self.ToShortString(), message.GetType());
            base.Respond(message);
        }

        public override void Send(PID target, Object message) {
            _log.Log(_level, "Agent {agent} sending `{messageType}` to {target}", Self.ToShortString(), message.GetType(), target.ToShortString());
            base.Send(target, message);
        }

        public override PID Spawn(Props props) {
            _log.Log(_level, "Agent {agent} spawning child agent", Self.ToShortString());
            return base.Spawn(props);
        }

        public override PID SpawnNamed(Props props, String name) {
            _log.Log(_level, "Agent {agent} spawning child agent with name `{child}`", Self.ToShortString(), name);
            return base.SpawnNamed(props, name);
        }

        public override PID SpawnPrefix(Props props, String prefix) {
            _log.Log(_level, "Agent {agent} spawning child agent with prefix `{prefix}`", Self.ToShortString(), prefix);
            return base.SpawnPrefix(props, prefix);
        }
    }
}
