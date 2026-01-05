namespace PairAdmin.Tests.Utilities;

/// <summary>
/// Helpers for async testing
/// </summary>
public static class AsyncTestHelpers
{
    /// <summary>
    /// Wait for a condition with timeout
    /// </summary>
    public static async Task<bool> WaitForAsync(
        Func<bool> condition,
        int timeoutMs = 5000,
        int pollIntervalMs = 100)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(timeoutMs))
        {
            if (condition())
                return true;

            await Task.Delay(pollIntervalMs);
        }

        return false;
    }

    /// <summary>
    /// Wait for a condition to become true with timeout
    /// </summary>
    public static async Task<T> WaitForResultAsync<T>(
        Func<Task<T>> getResult,
        Func<T, bool> condition,
        int timeoutMs = 5000,
        int pollIntervalMs = 100)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(timeoutMs))
        {
            var result = await getResult();
            if (condition(result))
                return result;

            await Task.Delay(pollIntervalMs);
        }

        throw new TimeoutException("Wait condition was not met within timeout");
    }

    /// <summary>
    /// Execute with retry on failure
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        int delayMs = 100)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs * attempt);
                }
            }
        }

        throw lastException!;
    }

    /// <summary>
    /// Create a cancelled cancellation token
    /// </summary>
    public static CancellationToken CreateCancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    /// <summary>
    /// Create a timeout cancellation token
    /// </summary>
    public static CancellationToken CreateTimeoutToken(int timeoutMs)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(timeoutMs);
        return cts.Token;
    }

    /// <summary>
    /// Execute an async action with timeout
    /// </summary>
    public static async Task<T> WithTimeoutAsync<T>(
        Func<Task<T>> action,
        int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            return await action().WithCancellation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
        }
    }

    /// <summary>
    /// Execute multiple tasks and return when all complete
    /// </summary>
    public static async Task<T[]> WhenAllFast<T>(params Func<Task<T>>[] tasks)
    {
        return await Task.WhenAll(tasks.Select(t => t()));
    }

    /// <summary>
    /// Execute multiple tasks and return first result
    /// </summary>
    public static async Task<T> WhenFirst<T>(params Func<Task<T>>[] tasks)
    {
        var taskList = tasks.Select(t => t()).ToList();
        var completion = new TaskCompletionSource<T>();

        foreach (var task in taskList)
        {
            task.ContinueWith(
                t => completion.TrySetResult(t.Result),
                TaskContinuationOptions.OnlyOnRanToCompletion);

            task.ContinueWith(
                t => completion.TrySetException(t.Exception!),
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(
                t => completion.TrySetCanceled(),
                TaskContinuationOptions.OnlyOnCanceled);
        }

        return await completion.Task;
    }
}
