namespace Lapine.Agents;

using System.Threading.Tasks.Dataflow;

static class DataBlockExtensions {
    static public CancellationTokenSource DelayPost<TInput>(this ITargetBlock<TInput> block, TInput item, TimeSpan delay) {
        if (block is null)
            throw new ArgumentNullException(nameof(block));

        if (item is null)
            throw new ArgumentNullException(nameof(item));

        var cancellationTokenSource = new CancellationTokenSource();

        Task.Delay(delay, cancellationTokenSource.Token)
            .ContinueWith(
                continuationFunction: _ => block.Post(item),
                cancellationToken   : cancellationTokenSource.Token,
                continuationOptions : TaskContinuationOptions.OnlyOnRanToCompletion,
                scheduler           : TaskScheduler.Current
            );

        return cancellationTokenSource;
    }
}
