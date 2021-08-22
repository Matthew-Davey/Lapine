namespace Lapine;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

static public class EnumerableExtensions {
    static public IImmutableQueue<T> ToImmutableQueue<T>(this IEnumerable<T> items) =>
        items.Aggregate(
            seed: ImmutableQueue<T>.Empty,
            func: (accumulator, item) => accumulator.Enqueue(item)
        );
}
