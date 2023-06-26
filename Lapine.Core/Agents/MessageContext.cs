namespace Lapine.Agents;

readonly record struct MessageContext(IAgent Self, Behaviour Behaviour, Object Message);
