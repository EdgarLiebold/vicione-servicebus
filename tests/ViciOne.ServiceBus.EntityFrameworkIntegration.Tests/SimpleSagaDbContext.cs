// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration.Tests
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using ViciOne.ServiceBus.Tests.Saga;


    public class SimpleSagaDbContext : SagaDbContext
    {
        public SimpleSagaDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new SimpleSagaMap(); }
        }


        class SimpleSagaMap :
            SagaClassMap<SimpleSaga>
        {
            protected override void Configure(EntityTypeConfiguration<SimpleSaga> entity, DbModelBuilder modelBuilder)
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(40);
                entity.Property(x => x.Initiated);
                entity.Property(x => x.Observed);
                entity.Property(x => x.Completed);
            }
        }
    }
}
