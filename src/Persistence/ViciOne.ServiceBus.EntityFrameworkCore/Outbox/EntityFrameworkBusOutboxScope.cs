using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Provides a distinct notification and options identity for one bus/DbContext outbox.</summary>
internal sealed class EntityFrameworkBusOutboxScope<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
