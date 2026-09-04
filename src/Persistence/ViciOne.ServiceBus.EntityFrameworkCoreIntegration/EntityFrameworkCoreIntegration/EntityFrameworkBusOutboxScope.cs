using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal sealed class EntityFrameworkBusOutboxScope<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
