namespace Lapine.Client;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public readonly record struct ExchangeDefinition(
    String Name,
    String Type,
    Durability Durability,
    Boolean AutoDelete,
    Boolean Internal,
    IReadOnlyDictionary<String, Object> Arguments
) {
    static public ExchangeDefinition Create(String name, String type) => new (
        Name      : name,
        Type      : type,
        Durability: Durability.Durable,
        AutoDelete: false,
        Internal  : false,
        Arguments : ImmutableDictionary<String, Object>.Empty
    );

    static public ExchangeDefinition Direct(String name) =>
        Create(name, "direct");

    static public ExchangeDefinition Fanout(String name) =>
        Create(name, "fanout");

    static public ExchangeDefinition Headers(String name) =>
        Create(name, "headers");

    static public ExchangeDefinition Topic(String name) =>
        Create(name, "topic");
};
