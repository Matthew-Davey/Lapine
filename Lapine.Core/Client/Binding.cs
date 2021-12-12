namespace Lapine.Client;

using System.Collections.Immutable;

public readonly record struct Binding(
    String Exchange,
    String Queue,
    String RoutingKey,
    IReadOnlyDictionary<String, Object> Arguments) {

    public const String DefaultRoutingKey = "#";

    static public Binding Create(String exchange, String queue, String routingKey = DefaultRoutingKey) => new(
        Exchange  : exchange,
        Queue     : queue,
        RoutingKey: routingKey,
        Arguments : ImmutableDictionary<String, Object>.Empty
    );
};
