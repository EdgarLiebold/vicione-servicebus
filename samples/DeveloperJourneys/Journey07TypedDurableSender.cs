namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey07TypedDurableSender
{
    public static IServiceCollection Configure(IServiceCollection services) => services
        .AddViciOneMessageContracts(catalog => catalog.Register<SubmitOrder>("orders.submit"))
        .AddViciOneServiceBus<IOrdersBus>("orders-v1", configuration =>
        {
            configuration.UsingInMemory();
            configuration.UseDurableSender(durable => durable.UseInMemoryStore());
        });

    public static Task<DurableSendReceipt> Send(
        IDurableSender<IOrdersBus> sender,
        SubmitOrder command,
        CancellationToken cancellationToken) =>
        sender.SendAsync(
            new Uri("loopback://orders/submit"),
            command,
            new DurableSendOptions { IdempotencyKey = new DurableSendId(command.OrderId) },
            cancellationToken);
}
