namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey11ScheduledMessage
{
    public static Task<ScheduledMessage<SubmitOrder>> ScheduleAsync(
        IMessageScheduler scheduler,
        DateTimeOffset deliverAt,
        SubmitOrder command,
        CancellationToken cancellationToken = default) =>
        scheduler.ScheduleSendAsync(new Uri("queue:submit-order"), deliverAt, command, cancellationToken);
}
