namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;

internal sealed class ReceiveCompletionObserver(int expectedCount) : IReceiveObserver
{
    private readonly TaskCompletionSource<bool> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _completedCount;

    public Task Completed => _completed.Task;
    public int CompletedCount => Volatile.Read(ref _completedCount);

    public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

    public Task PostReceiveAsync(ReceiveContext context)
    {
        if (Interlocked.Increment(ref _completedCount) >= expectedCount)
            _completed.TrySetResult(true);

        return Task.CompletedTask;
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class => Task.CompletedTask;

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class => Task.CompletedTask;

    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        _completed.TrySetException(exception);
        return Task.CompletedTask;
    }
}
