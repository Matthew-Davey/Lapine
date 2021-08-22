namespace Lapine;

using System;
using System.Collections.Generic;

static class MemoryExtensions {
    static public IEnumerable<ReadOnlyMemory<Byte>> Split(this ReadOnlyMemory<Byte> value, Int32 stride) {
        do {
            switch (value.Length) {
                case var length when length <= stride: {
                    yield return value;
                    yield break;
                }
                case var length when length > stride: {
                    yield return value[..stride];
                    value = value[stride..];
                    continue;
                }
            }
        } while (value.Length > 0);
    }
}
