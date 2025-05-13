namespace Lapine.Agents;

delegate ValueTask<MessageContext<TProtocol>> Behaviour<TProtocol>(MessageContext<TProtocol> context);
