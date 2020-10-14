namespace Lapine.Client {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ExchangeDefinition {
        public String Name { get; }
        public String Type { get; }
        public Durability Durability { get; }
        public Boolean AutoDelete { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        private ExchangeDefinition(String name, String type, Durability durability, Boolean autoDelete, IReadOnlyDictionary<String, Object> arguments) {
            Name       = name ?? throw new ArgumentNullException(nameof(name));
            Type       = type ?? throw new ArgumentNullException(nameof(type));
            Durability = durability;
            AutoDelete = autoDelete;
            Arguments  = arguments;
        }

        static public ExchangeDefinition Create(String name, String type = "topic") => new ExchangeDefinition(
            name      : name,
            type      : type,
            durability: Durability.Durable,
            autoDelete: false,
            arguments : new Dictionary<String, Object>()
        );

        public ExchangeDefinition WithType(String type) => new ExchangeDefinition(
            name      : Name,
            type      : type,
            durability: Durability,
            autoDelete: AutoDelete,
            arguments : Arguments
        );

        public ExchangeDefinition WithDurability(Durability durability) => new ExchangeDefinition(
            name      : Name,
            type      : Type,
            durability: durability,
            autoDelete: AutoDelete,
            arguments : Arguments
        );

        public ExchangeDefinition WithAutoDelete(Boolean autoDelete = true) => new ExchangeDefinition(
            name      : Name,
            type      : Type,
            durability: Durability,
            autoDelete: autoDelete,
            arguments : Arguments
        );

        public ExchangeDefinition WithArgument(String key, Object value) => new ExchangeDefinition(
            name      : Name,
            type      : Type,
            durability: Durability,
            autoDelete: AutoDelete,
            arguments : new Dictionary<String, Object>(Enumerable.Append(Arguments, new KeyValuePair<String, Object>(key, value)))
        );
    }
}
