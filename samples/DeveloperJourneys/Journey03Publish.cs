namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey03Publish
{
    public static Task Publish(IPublishEndpoint publisher, OrderSubmitted message, CancellationToken cancellationToken) =>
        publisher.Publish(message, cancellationToken);
}
