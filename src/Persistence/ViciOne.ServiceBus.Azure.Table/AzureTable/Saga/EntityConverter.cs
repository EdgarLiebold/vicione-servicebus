using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Converts reference-type instances to and from Azure Table property dictionaries.</summary>
/// <typeparam name="T">The reference type materialized by the converter.</typeparam>
public class EntityConverter<T> :
    IEntityConverter<T>
    where T : class
{
    readonly IList<IEntityPropertyConverter<T>> _converters;

    /// <summary>Creates a converter from the ordered property converters for <typeparamref name="T"/>.</summary>
    /// <param name="converters">The converters that read and write individual instance properties.</param>
    public EntityConverter(IList<IEntityPropertyConverter<T>> converters)
    {
        _converters = converters;
    }

    /// <summary>Projects all configured properties of an instance into an Azure Table property dictionary.</summary>
    /// <param name="entity">The instance to project.</param>
    /// <returns>The Azure Table-compatible property names and values.</returns>
    public IDictionary<string, object> GetDictionary(T entity)
    {
        var entityProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _converters.Count; i++)
            _converters[i].FromEntity(entity, entityProperties);

        return entityProperties;
    }

    /// <summary>Creates an instance and populates it from persisted Azure Table properties.</summary>
    /// <param name="entityProperties">The persisted property names and values.</param>
    /// <returns>The materialized instance.</returns>
    public T GetObject(IDictionary<string, object> entityProperties)
    {
        var entity = Activator.CreateInstance(typeof(T)) as T
            ?? throw new InvalidOperationException($"Unable to create Azure Table entity type {typeof(T).FullName}.");

        for (var i = 0; i < _converters.Count; i++)
            _converters[i].ToEntity(entity, entityProperties);

        return entity;
    }
}
