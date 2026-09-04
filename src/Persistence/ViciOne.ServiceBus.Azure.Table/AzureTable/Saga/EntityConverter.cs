using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an entity converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class EntityConverter<T> :
    IEntityConverter<T>
    where T : class
{
    readonly IList<IEntityPropertyConverter<T>> _converters;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converters">The converters value.</param>
    public EntityConverter(IList<IEntityPropertyConverter<T>> converters)
    {
        _converters = converters;
    }

    /// <summary>
    /// Gets dictionary.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the operation.</returns>
    public IDictionary<string, object> GetDictionary(T entity)
    {
        var entityProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _converters.Count; i++)
            _converters[i].FromEntity(entity, entityProperties);

        return entityProperties;
    }

    /// <summary>
    /// Gets object.
    /// </summary>
    /// <param name="entityProperties">The entity properties value.</param>
    /// <returns>The result of the operation.</returns>
    public T GetObject(IDictionary<string, object> entityProperties)
    {
        var entity = Activator.CreateInstance(typeof(T)) as T
            ?? throw new InvalidOperationException($"Unable to create Azure Table entity type {typeof(T).FullName}.");

        for (var i = 0; i < _converters.Count; i++)
            _converters[i].ToEntity(entity, entityProperties);

        return entity;
    }
}
