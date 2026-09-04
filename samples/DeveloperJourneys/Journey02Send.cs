namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey02Send
{
    public static Task SendAsync(
        ISendEndpoint endpoint,
        SubmitOrder command,
        CancellationToken cancellationToken = default) =>
        endpoint.SendAsync(command, cancellationToken);
}
