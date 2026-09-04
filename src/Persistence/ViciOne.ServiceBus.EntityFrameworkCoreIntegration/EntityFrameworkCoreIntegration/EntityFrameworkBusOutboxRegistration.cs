using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal sealed class EntityFrameworkBusOutboxRegistration<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
