using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for entity framework saga repository.
/// </summary>
public interface IEntityFrameworkSagaRepository
{
    /// <summary>
    /// Adds saga class map to the configuration.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="sagaClassMap">The saga class map value.</param>
    void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
        where TSaga : class, ISaga;

    /// <summary>
    /// Gets db context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    DbContext GetDbContext();
}
