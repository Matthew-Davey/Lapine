namespace Lapine {
    using System;
    using System.Collections.Generic;

    static class ISetExtensions {
        static public void AddRange<T>(this ISet<T> list, IEnumerable<T> items) {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            foreach (var item in items)
                list.Add(item);
        }
    }
}
