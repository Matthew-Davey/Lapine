namespace Lapine.Agents;

delegate Context Behaviour(Context context);

delegate Context<TState> Behaviour<TState>(Context<TState> context);
