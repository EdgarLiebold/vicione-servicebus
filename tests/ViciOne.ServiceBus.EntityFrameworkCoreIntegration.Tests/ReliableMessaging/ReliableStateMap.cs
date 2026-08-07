// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.ReliableMessaging
{
    using ViciOne.ServiceBus.Tests.ReliableMessaging;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;


    public class ReliableStateMap :
        SagaClassMap<ReliableState>
    {
        protected override void Configure(EntityTypeBuilder<ReliableState> entity, ModelBuilder model)
        {
            entity.Property(x => x.CurrentState);
        }
    }
}
