using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

public class EntityConverter<T> :
    IEntityConverter<T>
    where T : class
{
    readonly IList<IEntityPropertyConverter<T>> _converters;

    public EntityConverter(IList<IEntityPropertyConverter<T>> converters)
    {
        _converters = converters;
    }

    public IDictionary<string, object> GetDictionary(T entity)
    {
        var entityProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _converters.Count; i++)
            _converters[i].FromEntity(entity, entityProperties);

        return entityProperties;
    }

    public T GetObject(IDictionary<string, object> entityProperties)
    {
        var entity = Activator.CreateInstance(typeof(T)) as T
            ?? throw new InvalidOperationException($"Unable to create Azure Table entity type {typeof(T).FullName}.");

        for (var i = 0; i < _converters.Count; i++)
            _converters[i].ToEntity(entity, entityProperties);

        return entity;
    }
}
