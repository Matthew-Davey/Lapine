namespace Lapine.Agents {
    using System;
    using System.Runtime.CompilerServices;

    record AsyncCommand {
        AsyncValueTaskMethodBuilder _promise = AsyncValueTaskMethodBuilder.Create();

        public ValueTaskAwaiter GetAwaiter() =>
            _promise.Task.GetAwaiter();

        public void SetResult() =>
            _promise.SetResult();

        public void SetException(Exception exception) =>
            _promise.SetException(exception);
    }

    record AsyncCommand<TResult> {
        AsyncValueTaskMethodBuilder<TResult> _promise = AsyncValueTaskMethodBuilder<TResult>.Create();

        public ValueTaskAwaiter<TResult> GetAwaiter() =>
            _promise.Task.GetAwaiter();

        public void SetResult(TResult result) =>
            _promise.SetResult(result);

        public void SetException(Exception exception) =>
            _promise.SetException(exception);
    }
}
