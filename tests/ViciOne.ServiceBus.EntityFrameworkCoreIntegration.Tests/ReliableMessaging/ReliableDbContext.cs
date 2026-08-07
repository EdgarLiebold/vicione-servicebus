// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.ReliableMessaging
{
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;


    public class ReliableDbContext :
        SagaDbContext
    {
        public ReliableDbContext(DbContextOptions<ReliableDbContext> options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new ReliableStateMap(); }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddTransactionalOutboxEntities();
        }
    }
}
