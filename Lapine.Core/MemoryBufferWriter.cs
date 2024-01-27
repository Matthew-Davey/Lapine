namespace Lapine;

using System.Buffers;

using static System.Math;

/// <summary>
/// Represents a heap-based, memory backed output sink into which data can be written.
/// </summary>
/// <typeparam name="T">The type of items in this <see cref="T:Lapine.MemoryBufferWriter`1" /> instance.</typeparam>
sealed class MemoryBufferWriter<T> : IBufferWriter<T>, IMemoryOwner<T>, IDisposable {
    const Int32 DefaultInitialCapacity = 1024;

    readonly MemoryPool<T> _pool;
    IMemoryOwner<T> _owner;
    Int32 _offset = 0;

    /// <summary>
    /// Creates an instance of a <see cref="T:Lapine.MemoryBufferWriter`1" /> to which data can
    /// be written, with the default initial capacity.
    /// </summary>
    public MemoryBufferWriter()
        : this(DefaultInitialCapacity, MemoryPool<T>.Shared) {
    }

    /// <summary>
    /// Creates an instance of a <see cref="T:Lapine.MemoryBufferWriter`1" /> to which data can
    /// be written, with a specified initial capacity.
    /// </summary>
    /// <param name="pool">The <see cref="T:System.Buffers.MemoryPool`1" /> from which to rent memory.</param>
    /// <exception cref="T:System.ArgumentNullException">pool is null.</exception>
    public MemoryBufferWriter(MemoryPool<T> pool)
        : this(DefaultInitialCapacity, pool) {
    }

    /// <summary>
    /// Creates an instance of a <see cref="T:Lapine.MemoryBufferWriter`1" /> to which data can
    /// be written, with a specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="T:System.ArgumentException">initialCapacity is less than or equal to 0.</exception>
    public MemoryBufferWriter(Int32 initialCapacity)
        : this(initialCapacity, MemoryPool<T>.Shared) {
    }

    /// <summary>
    /// Creates an instance of a <see cref="T:Lapine.MemoryBufferWriter`1" /> to which data can
    /// be written, with a specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <param name="pool">The <see cref="T:System.Buffers.MemoryPool`1" /> from which to rent memory.</param>
    /// <exception cref="T:System.ArgumentException">initialCapacity is less than or equal to 0.</exception>
    /// <exception cref="T:System.ArgumentNullException">pool is null.</exception>
    public MemoryBufferWriter(Int32 initialCapacity, MemoryPool<T> pool) {
        if (initialCapacity < 1)
            throw new ArgumentException("initialCapacity must be greater than 0", nameof(initialCapacity));

        if (pool is null)
            throw new ArgumentNullException(nameof(pool));

        _pool = pool;
        _owner = _pool.Rent(initialCapacity);
    }

    /// <inheritdoc />
    public Memory<T> Memory =>
        _owner.Memory;

    /// <summary>
    /// Gets the total amount of space within the underlying buffer.
    /// </summary>
    /// <returns>The total capacity of the underlying buffer.</returns>
    public Int32 Capacity =>
        _owner.Memory.Length;

    /// <summary>
    /// Gets the amount of available space that can be written to without forcing the
    /// underlying buffer to grow.
    /// </summary>
    /// <returns>The space available for writing without forcing the underlying buffer to grow.</returns>
    public Int32 FreeCapacity =>
        Capacity - _offset;

    /// <summary>
    /// Gets the amount of data written to the underlying buffer.
    /// </summary>
    /// <returns>The amount of data written to the underlying buffer.</returns>
    public Int32 WrittenCount =>
        _offset;

    /// <summary>
    /// Gets the <see cref="T:System.ReadOnlyMemory`1" /> that contains the data written to the underlying
    /// buffer so far.
    /// </summary>
    /// <returns>The data written to the underlying buffer.</returns>
    public ReadOnlyMemory<T> WrittenMemory =>
        _owner.Memory[0.._offset];

    /// <summary>
    /// Gets the <see cref="T:System.ReadOnlySpan`1" /> that contains the data written to the underlying
    /// buffer so far.
    /// </summary>
    /// <returns>The data written to the underlying buffer.</returns>
    public ReadOnlySpan<T> WrittenSpan =>
        WrittenMemory.Span;

    /// <inheritdoc />
    public void Advance(Int32 count) {
        if (count < 0)
            throw new ArgumentException("count must not be negative.", nameof(count));

        if (count > FreeCapacity)
            throw new InvalidOperationException("Cannot advance beyond the end of the underlying buffer.");

        _offset += count;
    }

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    public void Clear() =>
        _offset = 0;

    /// <inheritdoc />
    public Memory<T> GetMemory(Int32 sizeHint = 0) {
        if (sizeHint < 0)
            throw new ArgumentException("sizeHint must not be negative.", nameof(sizeHint));

        if (sizeHint == 0)
            sizeHint = Min(FreeCapacity, DefaultInitialCapacity);

        while (sizeHint > FreeCapacity)
            Embiggen();

        return _owner.Memory.Slice(_offset, sizeHint);
    }

    /// <inheritdoc />
    public Span<T> GetSpan(Int32 sizeHint = 0) =>
        GetMemory(sizeHint).Span;

    void Embiggen() {
        var replacement = _pool.Rent(Capacity * 2);
        WrittenMemory.CopyTo(replacement.Memory);
        _owner.Dispose();
        _owner = replacement;
    }

    /// <inheritdoc />
    public void Dispose() {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }
}
