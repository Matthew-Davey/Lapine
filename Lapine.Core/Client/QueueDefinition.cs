namespace Lapine.Client {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public sealed record QueueDefinition(
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
}
