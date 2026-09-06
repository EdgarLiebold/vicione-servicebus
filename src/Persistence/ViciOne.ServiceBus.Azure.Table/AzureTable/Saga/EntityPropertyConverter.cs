using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Maps one directly supported CLR property to an Azure Table property of the same name.</summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class EntityPropertyConverter<TEntity, TProperty> :
    IEntityPropertyConverter<TEntity>
    where TEntity : class
{
    readonly ITypeConverter<object, TProperty> _fromEntity;
    readonly string _name;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly ITypeConverter<TProperty, object> _toEntity;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>Creates a converter for the named readable and writable property.</summary>
    /// <param name="name">The CLR property name and persisted Azure Table property name.</param>
    public EntityPropertyConverter(string name)
    {
        _name = name;
        _read = ReadPropertyCache<TEntity>.GetProperty<TProperty>(name);
        _write = WritePropertyCache<TEntity>.GetProperty<TProperty>(name);

        _toEntity = EntityPropertyTypeConverter.Instance as ITypeConverter<TProperty, object>
            ?? throw new ArgumentException("Invalid property type");

        _fromEntity = EntityPropertyTypeConverter.Instance as ITypeConverter<object, TProperty>
            ?? throw new ArgumentException("Invalid property type");
    }

    /// <summary>Reads the persisted value, converts it to <typeparamref name="TProperty"/>, and assigns it to the instance.</summary>
    /// <param name="entity">The instance to populate.</param>
    /// <param name="entityProperties">The persisted Azure Table properties.</param>
    public void ToEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        if (entityProperties.TryGetValue(_name, out var entityProperty))
        {
            if (_toEntity.TryConvert(entityProperty, out var propertyValue))
            {
                _write.Set(entity, propertyValue);
                return;
            }

            throw new InvalidOperationException(
                $"Azure Table property '{_name}' cannot be converted to '{typeof(TProperty)}'.");
        }
    }

    /// <summary>Reads the instance property and adds its Azure Table-compatible value to the property dictionary.</summary>
    /// <param name="entity">The instance to read.</param>
    /// <param name="entityProperties">The destination Azure Table property dictionary.</param>
    public void FromEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        var propertyValue = _read.Get(entity);
        if (propertyValue is null)
            return;

        if (_fromEntity.TryConvert(propertyValue, out var entityProperty) && entityProperty != null)
        {
            entityProperties.Add(_name, entityProperty);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{_name}' of type '{typeof(TProperty)}' cannot be converted to an Azure Table value.");
    }
}
