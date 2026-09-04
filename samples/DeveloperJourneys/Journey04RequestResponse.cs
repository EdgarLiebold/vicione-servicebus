namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey04RequestResponse
{
    public static async Task<OrderStatus> Request(
        IRequestClient<GetOrder> client,
        GetOrder request,
        CancellationToken cancellationToken)
    {
        Response<OrderStatus> response = await client.GetResponse<OrderStatus>(request, cancellationToken);
        return response.Message;
    }
}
