namespace Lapine;

using System.Runtime.ExceptionServices;

abstract record Result<T> {
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fault(ExceptionDispatchInfo ExceptionDispatchInfo) : Result<T>;

    public Result<U> Map<U>(Func<T, U> fn) => this switch {
        Ok(var value)           => new Result<U>.Ok(fn(value)),
        Fault(var dispatchInfo) => new Result<U>.Fault(dispatchInfo)
    };

    public T ValueOrThrow() {
        switch (this) {
            case Ok (var value):
                return value;
            case Fault (var dispatchInfo): {
                dispatchInfo.Throw();
                throw dispatchInfo.SourceException;
            }
            default:
                throw new Exception("Impossible!");
        }
    }
}
