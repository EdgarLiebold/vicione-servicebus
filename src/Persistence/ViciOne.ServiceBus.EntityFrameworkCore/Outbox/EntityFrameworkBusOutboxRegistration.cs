using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkBusOutboxRegistration<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
