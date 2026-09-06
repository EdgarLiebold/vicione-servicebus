using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Maps one instance property to and from an Azure Table property dictionary.</summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IEntityPropertyConverter<in TEntity>
    where TEntity : class
{
    /// <summary>Populates the corresponding instance property from the persisted property dictionary.</summary>
    /// <param name="entity">The instance to populate.</param>
    /// <param name="entityProperties">The persisted Azure Table properties.</param>
    void ToEntity(TEntity entity, IDictionary<string, object> entityProperties);
    /// <summary>Adds the corresponding instance property to the persistence dictionary.</summary>
    /// <param name="entity">The instance to read.</param>
    /// <param name="entityProperties">The destination Azure Table property dictionary.</param>
    void FromEntity(TEntity entity, IDictionary<string, object> entityProperties);
}
