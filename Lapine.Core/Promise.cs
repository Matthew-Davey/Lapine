namespace Lapine;

using System.Runtime.CompilerServices;

public class Promise {
    readonly TaskCompletionSource _taskCompletionSource;

    public Promise() =>
        _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskAwaiter GetAwaiter() =>
        _taskCompletionSource.Task.GetAwaiter();

    public void Resolve() =>
        _taskCompletionSource.SetResult();

    public void Reject(Exception fault) =>
        _taskCompletionSource.SetException(fault);
}

public class Promise<TResult> {
    readonly TaskCompletionSource<TResult> _taskCompletionSource;

    public Promise() =>
        _taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskAwaiter<TResult> GetAwaiter() =>
        _taskCompletionSource.Task.GetAwaiter();

    public void Resolve(TResult result) =>
        _taskCompletionSource.SetResult(result);

    public void Reject(Exception fault) =>
        _taskCompletionSource.SetException(fault);
}
