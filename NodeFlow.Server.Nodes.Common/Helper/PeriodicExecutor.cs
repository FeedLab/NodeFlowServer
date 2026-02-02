namespace NodeSharp.Nodes.Common.Helper;

public static class PeriodicExecutor
{
    public static async Task DelayedPeriodicExecution(
        TimeSpan delay,
        TimeSpan interval,
        Action<double> action,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var totalDelayMs = delay.TotalMilliseconds;
        var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var percentComplete = totalDelayMs > 0
                    ? Math.Min(elapsed / totalDelayMs * 100, 100)
                    : 100;

                MainThread.BeginInvokeOnMainThread(() => action(percentComplete));

                if (percentComplete >= 100)
                    break;
            } while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            timer?.Dispose();
        }
    }

    public static async Task DelayedPeriodicExecution(
        TimeSpan delay,
        TimeSpan interval,
        Func<double, Task> asyncAction,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var totalDelayMs = delay.TotalMilliseconds;
        var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var percentComplete = totalDelayMs > 0
                    ? Math.Min(elapsed / totalDelayMs * 100, 100)
                    : 100;

                await MainThread.InvokeOnMainThreadAsync(() => asyncAction(percentComplete));

                if (percentComplete >= 100)
                    break;
            } while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            timer?.Dispose();
        }
    }
}
