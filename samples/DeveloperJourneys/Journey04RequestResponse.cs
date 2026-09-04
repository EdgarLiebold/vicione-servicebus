namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey04RequestResponse
{
    public static async Task<OrderStatus> RequestAsync(
        IRequestClient<GetOrder> client,
        GetOrder request,
        CancellationToken cancellationToken = default)
    {
        Response<OrderStatus> response = await client.GetResponseAsync<OrderStatus>(request, cancellationToken);
        return response.Message;
    }
}
