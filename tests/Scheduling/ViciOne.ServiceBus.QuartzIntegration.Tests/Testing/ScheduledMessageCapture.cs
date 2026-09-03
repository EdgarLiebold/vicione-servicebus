using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;

internal sealed class ScheduledMessageCapture(string payloadTypeName) : IConsumeObserver
{
    private readonly TaskCompletionSource<ScheduledMessageSnapshot> _scheduled =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ScheduledMessageSnapshot> Scheduled => _scheduled.Task;

    public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        if (context.Message is ScheduleMessage schedule && IsRequestedPayload(schedule.PayloadType))
        {
            _scheduled.TrySetResult(new ScheduledMessageSnapshot(
                schedule.TokenId,
                schedule.ScheduledTime,
                schedule.Destination,
                [.. schedule.PayloadType]));
        }

        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        if (context.Message is ScheduleMessage schedule && IsRequestedPayload(schedule.PayloadType))
            _scheduled.TrySetException(exception);

        return Task.CompletedTask;
    }

    private bool IsRequestedPayload(IEnumerable<string> payloadTypes) => payloadTypes.Any(type =>
        type.EndsWith($":{payloadTypeName}", StringComparison.Ordinal));
}

internal sealed record ScheduledMessageSnapshot(
    Guid TokenId,
    DateTime ScheduledTime,
    Uri Destination,
    string[] PayloadTypes);
