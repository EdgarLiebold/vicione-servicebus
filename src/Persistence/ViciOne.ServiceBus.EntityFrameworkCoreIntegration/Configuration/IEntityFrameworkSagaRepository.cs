// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;


    public interface IEntityFrameworkSagaRepository
    {
        void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
            where TSaga : class, ISaga;

        DbContext GetDbContext();
    }
}
