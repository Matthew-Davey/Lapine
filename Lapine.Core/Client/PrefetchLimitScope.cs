namespace Lapine.Client {
    public enum PrefetchLimitScope {
        /// <summary>
        /// The limit is applied separately to each new consumer on the channel.
        /// </summary>
        Consumer,
        /// <summary>
        /// The limit is shared across all consumers on the channel.
        /// </summary>
        Channel
    }
}
