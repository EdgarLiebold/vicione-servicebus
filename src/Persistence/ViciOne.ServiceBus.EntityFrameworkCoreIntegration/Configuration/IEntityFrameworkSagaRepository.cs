using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

namespace ViciOne.ServiceBus;

public interface IEntityFrameworkSagaRepository
{
    void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
        where TSaga : class, ISaga;

    DbContext GetDbContext();
}
