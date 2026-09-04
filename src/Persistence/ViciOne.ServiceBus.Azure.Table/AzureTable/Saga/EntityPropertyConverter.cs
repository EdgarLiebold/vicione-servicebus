using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an entity property converter implementation.
/// </summary>
/// <typeparam name="TEntity">The t entity type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class EntityPropertyConverter<TEntity, TProperty> :
    IEntityPropertyConverter<TEntity>
    where TEntity : class
{
    readonly ITypeConverter<object, TProperty> _fromEntity;
    readonly string _name;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly ITypeConverter<TProperty, object> _toEntity;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
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

    /// <summary>
    /// Performs the to entity operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityProperties">The entity properties value.</param>
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

    /// <summary>
    /// Performs the from entity operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityProperties">The entity properties value.</param>
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
