using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;

internal sealed class ScheduledMessageSequenceCapture : IConsumeObserver
{
    private readonly string _payloadTypeName;
    private readonly TaskCompletionSource<ScheduledMessageSnapshot>[] _scheduled;
    private int _observedCount;

    public ScheduledMessageSequenceCapture(string payloadTypeName, int expectedCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadTypeName);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedCount, 1);

        _payloadTypeName = payloadTypeName;
        _scheduled = Enumerable.Range(0, expectedCount)
            .Select(_ => new TaskCompletionSource<ScheduledMessageSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
    }

    public int ObservedCount => Volatile.Read(ref _observedCount);

    public Task<ScheduledMessageSnapshot> At(int zeroBasedIndex) => _scheduled[zeroBasedIndex].Task;

    public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        if (context.Message is ScheduleMessage schedule && IsRequestedPayload(schedule.PayloadType))
        {
            int index = Interlocked.Increment(ref _observedCount) - 1;
            if ((uint)index < (uint)_scheduled.Length)
            {
                _scheduled[index].TrySetResult(new ScheduledMessageSnapshot(
                    schedule.CorrelationId,
                    schedule.ScheduledTime,
                    schedule.Destination,
                    [.. schedule.PayloadType]));
            }
        }

        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        if (context.Message is ScheduleMessage schedule && IsRequestedPayload(schedule.PayloadType))
        {
            foreach (TaskCompletionSource<ScheduledMessageSnapshot> source in _scheduled)
                source.TrySetException(exception);
        }

        return Task.CompletedTask;
    }

    private bool IsRequestedPayload(IEnumerable<string> payloadTypes) => payloadTypes.Any(type =>
        type.EndsWith($":{_payloadTypeName}", StringComparison.Ordinal));
}
