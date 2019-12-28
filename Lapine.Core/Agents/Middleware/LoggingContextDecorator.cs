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
            _log.Log(_level, "Agent {agent} forwarding message to {target}", Self, target);
            base.Forward(target);
        }

        public override Task Receive(MessageEnvelope envelope) {
            _log.Log(_level, "Agent {agent} receiving message envelope from {sender} with message of type '{messageType}'", Self, envelope.Sender, envelope.Message.GetType());
            return base.Receive(envelope);
        }

        public override void Request(PID target, Object message) {
            _log.Log(_level, "Agent {agent} sending request message of type '{messageType}' to {target}", Self, message.GetType(), target);
            base.Request(target, message);
        }

        public override void Respond(Object message) {
            _log.Log(_level, "Agent {agent} responding to request message with reply of type '{messageType}'", Self, message.GetType());
            base.Respond(message);
        }

        public override void Send(PID target, Object message) {
            _log.Log(_level, "Agent {agent} sending message '{message}' to {target}", Self, message, target);
            base.Send(target, message);
        }

        public override PID Spawn(Props props) {
            _log.Log(_level, "Agent {agent} spawning child agent", Self);
            return base.Spawn(props);
        }

        public override PID SpawnNamed(Props props, String name) {
            _log.Log(_level, "Agent {agent} spawning child agent with name '{child}'", Self, name);
            return base.SpawnNamed(props, name);
        }

        public override PID SpawnPrefix(Props props, String prefix) {
            _log.Log(_level, "Agent {agent} spawning child agent with prefix '{prefix}'", Self, prefix);
            return base.SpawnPrefix(props, prefix);
        }
    }
}
