namespace Lapine.Client;

using System.Collections.Immutable;

public readonly record struct QueueDefinition(
    String Name,
    Durability Durability,
    Boolean Exclusive,
    Boolean AutoDelete,
    IReadOnlyDictionary<String, Object> Arguments
) {
    static public QueueDefinition Create(String name) => new (
        Name      : name,
        Durability: Durability.Durable,
        Exclusive : false,
        AutoDelete: false,
        Arguments : ImmutableDictionary<String, Object>.Empty
    );
}
