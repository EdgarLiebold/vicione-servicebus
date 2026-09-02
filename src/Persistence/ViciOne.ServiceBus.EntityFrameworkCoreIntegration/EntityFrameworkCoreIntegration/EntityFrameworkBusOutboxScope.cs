namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using Microsoft.EntityFrameworkCore;


internal sealed class EntityFrameworkBusOutboxScope<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
