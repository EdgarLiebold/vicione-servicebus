using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Tests.Testing;

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

    public Task<ScheduledMessageSnapshot> AtAsync(int zeroBasedIndex) => _scheduled[zeroBasedIndex].Task;

    public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
    {
        if (context.Message is ScheduleMessage schedule && IsRequestedPayload(schedule.PayloadType))
        {
            int index = Interlocked.Increment(ref _observedCount) - 1;
            if ((uint)index < (uint)_scheduled.Length)
            {
                _scheduled[index].TrySetResult(new ScheduledMessageSnapshot(
                    schedule.TokenId,
                    schedule.DueAt,
                    schedule.Destination,
                    [.. schedule.PayloadType]));
            }
        }

        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
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
