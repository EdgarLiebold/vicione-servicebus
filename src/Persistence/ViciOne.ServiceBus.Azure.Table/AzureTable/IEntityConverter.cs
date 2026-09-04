using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>
/// Defines the contract for entity converter.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IEntityConverter<T>
    where T : class
{
    /// <summary>
    /// Gets dictionary.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the operation.</returns>
    IDictionary<string, object> GetDictionary(T entity);
    /// <summary>
    /// Gets object.
    /// </summary>
    /// <param name="entityProperties">The entity properties value.</param>
    /// <returns>The result of the operation.</returns>
    T GetObject(IDictionary<string, object> entityProperties);
}
