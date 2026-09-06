using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>Converts instances to and from Azure Table property dictionaries.</summary>
/// <typeparam name="T">The reference type represented by the property dictionary.</typeparam>
public interface IEntityConverter<T>
    where T : class
{
    /// <summary>Projects an instance into Azure Table-compatible named properties.</summary>
    /// <param name="entity">The instance to project.</param>
    /// <returns>The property names and values to persist.</returns>
    IDictionary<string, object> GetDictionary(T entity);
    /// <summary>Materializes an instance from persisted Azure Table properties.</summary>
    /// <param name="entityProperties">The persisted property names and values.</param>
    /// <returns>The materialized instance.</returns>
    T GetObject(IDictionary<string, object> entityProperties);
}
