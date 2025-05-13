namespace Lapine;

static class TaskExtensions {
    static public Task ContinueWith(this Task task, Action onCompleted, Action<Exception> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                onCompleted();
            }
            else {
                onFaulted(antecedent.Exception!.InnerException!);
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    static public Task ContinueWith<T>(this Task<T> task, Action<T> onCompleted, Action<Exception> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                onCompleted(task.Result);
            }
            else {
                onFaulted(antecedent.Exception!.InnerException!);
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    static public Task ContinueWith(this Task task, Func<Task> onCompleted, Action<Exception> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(async antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                await onCompleted();
            }
            else {
                onFaulted(antecedent.Exception!.InnerException!);
            }
        }).Unwrap();
    }

    static public Task ContinueWith<T>(this Task<T> task, Func<T, Task> onCompleted, Action<Exception> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(async antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                await onCompleted(antecedent.Result);
            }
            else {
                onFaulted(antecedent.Exception!.InnerException!);
            }
        }).Unwrap();
    }

    static public Task<U> ContinueWith<U>(this Task task, Func<U> onCompleted, Func<Exception, U> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                return Task.FromResult(onCompleted());
            }
            else {
                return Task.FromResult(onFaulted(antecedent.Exception!.InnerException!));
            }
        }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
    }

    static public Task<U> ContinueWith<T, U>(this Task<T> task, Func<T, U> onCompleted, Func<Exception, U> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                return Task.FromResult(onCompleted(task.Result));
            }
            else {
                return Task.FromResult(onFaulted(antecedent.Exception!.InnerException!));
            }
        }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
    }

    static public Task<U> ContinueWith<T, U>(this Task<T> task, Func<T, Task<U>> onCompleted, Func<Exception, U> onFaulted) {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onCompleted);
        ArgumentNullException.ThrowIfNull(onFaulted);

        return task.ContinueWith(async antecedent => {
            if (antecedent.IsCompletedSuccessfully) {
                return await onCompleted(task.Result);
            }
            else {
                return onFaulted(antecedent.Exception!.InnerException!);
            }
        }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
    }
}
