namespace Lapine;

using System.Collections.Immutable;

static public class EnumerableExtensions {
    static public IImmutableQueue<T> ToImmutableQueue<T>(this IEnumerable<T> items) =>
        items.Aggregate(
            seed: ImmutableQueue<T>.Empty,
            func: (accumulator, item) => accumulator.Enqueue(item)
        );
}
