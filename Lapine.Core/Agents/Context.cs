namespace Lapine.Agents;

readonly record struct Context(Object? Message, Behaviour Behaviour);

readonly record struct Context<TState>(Object? Message, Behaviour<TState> Behaviour, TState State);
