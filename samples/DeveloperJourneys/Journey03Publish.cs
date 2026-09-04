namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey03Publish
{
    public static Task PublishAsync(
        IPublishEndpoint publisher,
        OrderSubmitted message,
        CancellationToken cancellationToken = default) =>
        publisher.PublishAsync(message, cancellationToken);
}
