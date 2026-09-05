using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey06TransactionalOutbox
{
    public static IServiceCollection Configure(IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<JourneyDbContext>(options => options.UseSqlite(connectionString));
        return services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory();
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
                reliable.AddMessageContract<SubmitOrder>("orders.submit");
            });
        });
    }
}
