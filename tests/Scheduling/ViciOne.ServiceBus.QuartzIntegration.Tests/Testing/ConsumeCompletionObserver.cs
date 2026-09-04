namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;

internal sealed class ConsumeCompletionObserver<TMessage>(Func<TMessage, bool> predicate, int expectedCount = 1) : IConsumeObserver
    where TMessage : class
{
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int _expectedCount = expectedCount > 0
        ? expectedCount
        : throw new ArgumentOutOfRangeException(nameof(expectedCount), expectedCount, "Expected count must be greater than zero.");
    private int _observedCount;

    public Task Completed => _completed.Task;
    public int ObservedCount => Volatile.Read(ref _observedCount);

    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class => Task.CompletedTask;

    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        if (context.Message is TMessage message
            && predicate(message)
            && Interlocked.Increment(ref _observedCount) == _expectedCount)
        {
            _completed.TrySetResult();
        }

        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        if (context.Message is TMessage message && predicate(message))
            _completed.TrySetException(exception);

        return Task.CompletedTask;
    }
}
