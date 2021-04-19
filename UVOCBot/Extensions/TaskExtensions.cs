namespace System.Threading.Tasks
{
#nullable disable
    public static class TaskExtensions
    {
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default)
                await task.ConfigureAwait(false);

            TaskCompletionSource<bool> tcs = new();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                    throw new OperationCanceledException(cancellationToken);
            }
        }

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default)
                return await task.ConfigureAwait(false);

            TaskCompletionSource<bool> tcs = new();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                    throw new OperationCanceledException(cancellationToken);
            }

            return task.Result;
        }
    }
#nullable restore
}
