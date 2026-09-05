using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey17ReliableMessagingInbox
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<SubmitOrderConsumer>();
            configuration.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 10_000,
                    MaximumStoredBytes = 16 * 1024 * 1024,
                });
                reliable.Delivery(delivery =>
                {
                    delivery.MaximumAttempts = 5;
                    delivery.InitialRetryDelay = TimeSpan.FromMilliseconds(100);
                    delivery.MaximumRetryDelay = TimeSpan.FromSeconds(5);
                });
                reliable.Retention(TimeSpan.FromDays(7));
                reliable.AddMessageContract<SubmitOrder>("orders.submit");
            });
        });

    public static Task<ReliableInboxQuarantinePage> ReadInboxQuarantineAsync(
        IReliableMessagingOperations<IBus> operations,
        CancellationToken cancellationToken = default) =>
        operations.GetInboxQuarantineAsync(
            new ReliableInboxQuarantineQuery { PageSize = 100 },
            cancellationToken);
}
