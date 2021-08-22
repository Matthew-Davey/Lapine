namespace Lapine;

using System;
using System.Threading.Tasks;

static class TaskExtensions {
    static public Task ContinueWith(this Task task, Action? onCompleted = null, Action<Exception>? onFaulted = null, Action? onCancelled = null) =>
        task.ContinueWith(antecedent => {
            switch (antecedent.Status) {
                case TaskStatus.RanToCompletion: {
                    onCompleted?.Invoke();
                    break;
                }
                case TaskStatus.Faulted: {
                    onFaulted?.Invoke(antecedent.Exception!.GetBaseException());
                    break;
                }
                case TaskStatus.Canceled: {
                    onCancelled?.Invoke();
                    break;
                }
            }
        });

    static public Task ContinueWith<TResult>(this Task<TResult> task, Action<TResult>? onCompleted = null, Action<Exception>? onFaulted = null, Action? onCancelled = null) =>
        task.ContinueWith(antecedent => {
            switch (antecedent.Status) {
                case TaskStatus.RanToCompletion: {
                    onCompleted?.Invoke(antecedent.Result);
                    break;
                }
                case TaskStatus.Faulted: {
                    onFaulted?.Invoke(antecedent.Exception!.GetBaseException());
                    break;
                }
                case TaskStatus.Canceled: {
                    onCancelled?.Invoke();
                    break;
                }
            }
        });
}
