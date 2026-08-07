// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;


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
}
