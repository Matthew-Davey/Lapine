namespace Lapine {
    using System;
    using System.Collections.Generic;

    static class MemoryExtensions {
        static public IEnumerable<ReadOnlyMemory<Byte>> Split(this ReadOnlyMemory<Byte> value, Int32 stride) {
            while (value.Length > 0) {
                switch (value.Length) {
                    case var length when length <= stride: {
                        yield return value;
                        yield break;
                    }
                    case var length when length > stride: {
                        var segment = value.Slice(0, stride);
                        yield return segment;
                        value = value[stride..];
                        break;
                    }
                }
            }
        }
    }
}
