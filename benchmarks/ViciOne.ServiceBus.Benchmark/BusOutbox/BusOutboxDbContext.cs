using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOneServiceBusBenchmark.BusOutbox;

public class BusOutboxDbContext :
    SagaDbContext
{
    public BusOutboxDbContext(DbContextOptions<BusOutboxDbContext> options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield break; }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddTransactionalOutboxEntities();
    }
}
