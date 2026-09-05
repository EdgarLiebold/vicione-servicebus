using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey18SuiteComposition
{
    public static IServiceCollection Configure(
        IServiceCollection services,
        string sqliteConnectionString,
        string rabbitMqHost,
        string user,
        string password)
    {
        services.AddPooledDbContextFactory<JourneyDbContext>(options => options.UseSqlite(sqliteConnectionString));
        return services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<GetOrderConsumer>();
            configuration.AddRequestClient<GetOrder>(new Uri("queue:get-order"));
            configuration.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(rabbitMqHost, credentials =>
                {
                    credentials.Username(user);
                    credentials.Password(password);
                });
                rabbit.ConfigureEndpoints(context);
            });
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseEntityFramework<JourneyDbContext>();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 10_000,
                    MaximumStoredBytes = 16 * 1024 * 1024,
                });
                reliable.Delivery(_ => { });
                reliable.Retention(TimeSpan.FromDays(7));
                reliable.AddMessageContract<GetOrder>("orders.get");
                reliable.AddMessageContract<OrderStatus>("orders.status");
                reliable.AddMessageContract<SubmitOrder>("orders.submit");
            });
        });
    }

    public static Task<ScheduledMessage<SubmitOrder>> ScheduleAsync(
        IMessageScheduler scheduler,
        SubmitOrder command,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default) =>
        scheduler.ScheduleSendAsync(new Uri("queue:submit-order"), dueAt, command, cancellationToken);
}
