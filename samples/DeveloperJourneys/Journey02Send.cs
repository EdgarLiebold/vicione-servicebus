namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey02Send
{
    public static Task Send(ISendEndpointProvider endpoints, SubmitOrder command, CancellationToken cancellationToken) =>
        endpoints.Send(command, cancellationToken);
}
