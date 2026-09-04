using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey06TransactionalOutbox
{
    public static IServiceCollection Configure(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<JourneyDbContext>(options => options.UseSqlite(connectionString));
        return services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddEntityFrameworkOutbox<JourneyDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.UseBusOutbox();
            });
            configuration.UsingInMemory();
        });
    }
}
