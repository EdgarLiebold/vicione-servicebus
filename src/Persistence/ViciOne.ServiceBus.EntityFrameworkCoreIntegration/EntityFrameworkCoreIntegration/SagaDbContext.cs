using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public abstract class SagaDbContext :
    DbContext
{
    protected SagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected abstract IEnumerable<ISagaClassMap> Configurations { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var configuration in Configurations)
            configuration.Configure(modelBuilder);
    }
}
