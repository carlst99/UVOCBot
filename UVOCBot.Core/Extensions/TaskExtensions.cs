// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks;

public static class TaskExtensions
{
    public static async Task WithCancellation(this Task task, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
            await task;

        TaskCompletionSource<bool> tcs = new();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
                throw new OperationCanceledException(cancellationToken);
        }
    }

    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
            return await task;

        TaskCompletionSource<bool> tcs = new();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
                throw new OperationCanceledException(cancellationToken);
        }

        return task.Result;
    }
}
