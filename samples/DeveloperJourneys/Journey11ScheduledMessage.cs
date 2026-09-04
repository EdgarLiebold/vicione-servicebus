namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey11ScheduledMessage
{
    public static Task<ScheduledMessage<SubmitOrder>> Schedule(
        IMessageScheduler scheduler,
        DateTimeOffset deliverAt,
        SubmitOrder command,
        CancellationToken cancellationToken) =>
        scheduler.ScheduleSend(new Uri("queue:submit-order"), deliverAt.UtcDateTime, command, cancellationToken);
}
