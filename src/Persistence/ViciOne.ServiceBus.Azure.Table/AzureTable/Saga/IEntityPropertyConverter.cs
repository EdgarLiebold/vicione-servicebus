using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Defines the contract for entity property converter.
/// </summary>
/// <typeparam name="TEntity">The t entity type.</typeparam>
public interface IEntityPropertyConverter<in TEntity>
    where TEntity : class
{
    /// <summary>
    /// Performs the to entity operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityProperties">The entity properties value.</param>
    void ToEntity(TEntity entity, IDictionary<string, object> entityProperties);
    /// <summary>
    /// Performs the from entity operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityProperties">The entity properties value.</param>
    void FromEntity(TEntity entity, IDictionary<string, object> entityProperties);
}
