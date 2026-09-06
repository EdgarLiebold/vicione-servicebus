using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Maps one value-type property through the object serializer to an Azure Table string property.</summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ValueTypeEntityPropertyConverter<TEntity, TProperty> :
    IEntityPropertyConverter<TEntity>
    where TEntity : class
    where TProperty : struct
{
    readonly string _name;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>Creates a serialized converter for the named readable and writable property.</summary>
    /// <param name="name">The CLR property name and persisted Azure Table property name.</param>
    public ValueTypeEntityPropertyConverter(string name)
    {
        _name = name;
        _read = ReadPropertyCache<TEntity>.GetProperty<TProperty>(name);
        _write = WritePropertyCache<TEntity>.GetProperty<TProperty>(name);
    }

    /// <summary>Deserializes the persisted string and assigns a valid value to the instance property.</summary>
    /// <param name="entity">The instance to populate.</param>
    /// <param name="entityProperties">The persisted Azure Table properties.</param>
    public void ToEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        if (entityProperties.TryGetValue(_name, out var entityProperty))
        {
            TProperty? propertyValue = ObjectDeserializer.Deserialize<TProperty>(entityProperty.ToString());

            if (propertyValue.HasValue)
                _write.Set(entity, propertyValue.Value);
        }
    }

    /// <summary>Serializes the instance property and adds a nonblank representation to the persistence dictionary.</summary>
    /// <param name="entity">The instance to read.</param>
    /// <param name="entityProperties">The destination Azure Table property dictionary.</param>
    public void FromEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        var propertyValue = _read.Get(entity);

        var text = ObjectDeserializer.Serialize(propertyValue);
        if (!string.IsNullOrWhiteSpace(text))
            entityProperties.Add(_name, text);
    }
}
