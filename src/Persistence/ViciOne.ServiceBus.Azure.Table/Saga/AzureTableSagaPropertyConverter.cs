using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Maps one natively supported saga property to an isolated Azure Table property.</summary>
/// <typeparam name="TEntity">The saga state containing the property.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class AzureTableSagaPropertyConverter<TEntity, TProperty> :
    IAzureTableSagaPropertyConverter<TEntity>
    where TEntity : class
{
    readonly ITypeConverter<object, TProperty> _fromEntity;
    readonly string _propertyName;
    readonly string _storageName;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly ITypeConverter<TProperty, object> _toEntity;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>Creates a converter for the named readable and writable property.</summary>
    /// <param name="propertyName">The CLR property name.</param>
    /// <param name="storageName">The non-reserved Azure Table property name.</param>
    public AzureTableSagaPropertyConverter(string propertyName, string storageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        AzureTableSagaPropertyValidator.ValidateStorageName(storageName, nameof(storageName));

        _propertyName = propertyName;
        _storageName = storageName;
        _read = ReadPropertyCache<TEntity>.GetProperty<TProperty>(propertyName);
        _write = WritePropertyCache<TEntity>.GetProperty<TProperty>(propertyName);

        _toEntity = AzureTablePropertyTypeConverter.Instance as ITypeConverter<TProperty, object>
            ?? throw new ArgumentException(
                $"Property '{propertyName}' of type '{typeof(TProperty)}' is not supported as a native Azure Table value.",
                nameof(propertyName));

        _fromEntity = AzureTablePropertyTypeConverter.Instance as ITypeConverter<object, TProperty>
            ?? throw new ArgumentException(
                $"Property '{propertyName}' of type '{typeof(TProperty)}' cannot be materialized from a native Azure Table value.",
                nameof(propertyName));
    }

    public string StorageName => _storageName;

    /// <summary>Reads the persisted value, converts it to <typeparamref name="TProperty"/>, and assigns it to the instance.</summary>
    /// <param name="entity">The instance to populate.</param>
    /// <param name="entityProperties">The persisted Azure Table properties.</param>
    public void ToEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entityProperties);

        if (entityProperties.TryGetValue(_storageName, out var entityProperty))
        {
            if (_toEntity.TryConvert(entityProperty, out var propertyValue))
            {
                _write.Set(entity, propertyValue);
                return;
            }

            throw new InvalidOperationException(
                $"Azure Table property '{_storageName}' cannot be converted to '{typeof(TProperty)}'.");
        }
    }

    /// <summary>Reads the instance property and adds its Azure Table-compatible value to the property dictionary.</summary>
    /// <param name="entity">The instance to read.</param>
    /// <param name="entityProperties">The destination Azure Table property dictionary.</param>
    public void FromEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entityProperties);

        var propertyValue = _read.Get(entity);
        if (propertyValue is null)
            return;

        if (_fromEntity.TryConvert(propertyValue, out var entityProperty) && entityProperty != null)
        {
            entityProperties.Add(
                _storageName,
                AzureTableSagaPropertyValidator.ValidateValue(entityProperty, _propertyName));
            return;
        }

        throw new InvalidOperationException(
            $"Saga property '{_propertyName}' of type '{typeof(TProperty)}' cannot be converted to Azure Table property '{_storageName}'.");
    }
}
