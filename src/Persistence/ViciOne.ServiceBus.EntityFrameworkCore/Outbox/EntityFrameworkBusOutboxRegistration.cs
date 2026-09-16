using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Marks one bus/DbContext pair as owning an EF transactional-outbox registration.</summary>
internal sealed class EntityFrameworkBusOutboxRegistration<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
}
