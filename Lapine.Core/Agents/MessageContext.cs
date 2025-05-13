namespace Lapine.Agents;

readonly record struct MessageContext<TProtocol>(IAgent<TProtocol> Self, Behaviour<TProtocol> Behaviour, TProtocol Message);
