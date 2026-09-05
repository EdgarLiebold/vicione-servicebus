using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey11ScheduledMessage
{
    public static IServiceCollection Configure(IServiceCollection services) => services
        .AddViciOneServiceBus(configuration =>
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

    public static Task<ScheduledMessage<SubmitOrder>> ScheduleAsync(
        IMessageScheduler scheduler,
        DateTimeOffset deliverAt,
        SubmitOrder command,
        CancellationToken cancellationToken = default) =>
        scheduler.ScheduleSendAsync(new Uri("queue:submit-order"), deliverAt, command, cancellationToken);
}
