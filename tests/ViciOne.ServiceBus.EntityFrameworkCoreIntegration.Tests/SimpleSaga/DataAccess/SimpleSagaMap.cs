// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.SimpleSaga.DataAccess
{
    using ViciOne.ServiceBus.Tests.Saga;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;


    class SimpleSagaMap : SagaClassMap<SimpleSaga>
    {
        protected override void Configure(EntityTypeBuilder<SimpleSaga> entity, ModelBuilder model)
        {
            entity.Property(x => x.Name).HasMaxLength(40);
            entity.Property(x => x.Initiated);
            entity.Property(x => x.Observed);
            entity.Property(x => x.Completed);

            entity.ToTable("EfCoreSimpleSagas");
        }
    }
}
