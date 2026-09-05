using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey07TypedDurableSender
{
    public static IServiceCollection Configure(IServiceCollection services) => services
        .AddViciOneServiceBus<IOrdersBus>("orders-v1", configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory();
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 10_000,
                    MaximumStoredBytes = 16 * 1024 * 1024,
                });
                reliable.Delivery(_ => { });
                reliable.Retention(TimeSpan.FromDays(7));
                reliable.AddMessageContract<SubmitOrder>("orders.submit");
            });
        });

    public static Task<DurableSendReceipt> SendAsync(
        IDurableSender<IOrdersBus> sender,
        SubmitOrder command,
        CancellationToken cancellationToken = default) =>
        sender.SendAsync(
            new Uri("loopback://orders/submit"),
            command,
            new DurableSendOptions { IdempotencyKey = new DurableSendId(command.OrderId) },
            cancellationToken);
}
