namespace Lapine.Client {
    using System;
    using System.Collections.Generic;

    public sealed record ExchangeDefinition(
        String Name,
        String Type,
        Durability Durability,
        Boolean AutoDelete,
        IReadOnlyDictionary<String, Object> Arguments
    ) {
        static public ExchangeDefinition Create(String name, String type = "topic") => new (
            Name      : name,
            Type      : type,
            Durability: Durability.Durable,
            AutoDelete: false,
            Arguments : new Dictionary<String, Object>()
        );
    };
}
