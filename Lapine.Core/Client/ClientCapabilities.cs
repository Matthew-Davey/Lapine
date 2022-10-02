namespace Lapine.Client;

public readonly record struct ClientCapabilities(Boolean BasicNack, Boolean PublisherConfirms) {
    static public ClientCapabilities None => new(
        BasicNack        : false,
        PublisherConfirms: false
    );

    static public ClientCapabilities Default => new(
        BasicNack        : true,
        PublisherConfirms: true
    );

    public IReadOnlyDictionary<String, Object> ToDictionary() => new Dictionary<String, Object> {
        ["basic_nack"]         = BasicNack,
        ["publisher_confirms"] = PublisherConfirms
    };
}
