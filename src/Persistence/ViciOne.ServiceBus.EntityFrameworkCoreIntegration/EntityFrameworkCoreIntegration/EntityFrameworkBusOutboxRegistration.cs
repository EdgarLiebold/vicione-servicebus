namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using Microsoft.EntityFrameworkCore;


internal sealed class EntityFrameworkBusOutboxRegistration<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
