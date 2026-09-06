using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Collects saga entity mappings and creates DbContext instances that apply them.</summary>
public interface IEntityFrameworkSagaRepository
{
    /// <summary>Adds the EF Core mapping for a saga type.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="sagaClassMap">The mapping to add.</param>
    void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
        where TSaga : class, ISaga;

    /// <summary>Creates a DbContext whose model contains all registered saga mappings.</summary>
    /// <returns>A new DbContext owned by the caller.</returns>
    DbContext GetDbContext();
}
