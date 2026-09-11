using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Maps one reference-type saga property through serialization to an isolated Azure Table string property.</summary>
/// <typeparam name="TEntity">The saga state containing the property.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class SerializedReferenceAzureTableSagaPropertyConverter<TEntity, TProperty> :
    IAzureTableSagaPropertyConverter<TEntity>
    where TEntity : class
    where TProperty : class
{
    readonly string _propertyName;
    readonly string _storageName;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>Creates a serialized converter for the named readable and writable property.</summary>
    /// <param name="propertyName">The CLR property name.</param>
    /// <param name="storageName">The non-reserved Azure Table property name.</param>
    public SerializedReferenceAzureTableSagaPropertyConverter(string propertyName, string storageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        AzureTableSagaPropertyValidator.ValidateStorageName(storageName, nameof(storageName));

        _propertyName = propertyName;
        _storageName = storageName;
        _read = ReadPropertyCache<TEntity>.GetProperty<TProperty>(propertyName);
        _write = WritePropertyCache<TEntity>.GetProperty<TProperty>(propertyName);
    }

    public string StorageName => _storageName;

    /// <summary>Deserializes the persisted string and assigns it to the instance property.</summary>
    /// <param name="entity">The instance to populate.</param>
    /// <param name="entityProperties">The persisted Azure Table properties.</param>
    public void ToEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entityProperties);

        if (entityProperties.TryGetValue(_storageName, out var entityProperty))
        {
            if (entityProperty is not string serializedValue)
            {
                throw new InvalidOperationException(
                    $"Azure Table property '{_storageName}' must contain serialized text for saga property '{_propertyName}' of type '{typeof(TProperty)}'.");
            }

            var propertyValue = ServiceBusMetadataSerializer.DeserializeReference<TProperty>(serializedValue);

            _write.Set(entity, propertyValue);
        }
    }

    /// <summary>Serializes the instance property and adds a nonblank representation to the persistence dictionary.</summary>
    /// <param name="entity">The instance to read.</param>
    /// <param name="entityProperties">The destination Azure Table property dictionary.</param>
    public void FromEntity(TEntity entity, IDictionary<string, object> entityProperties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entityProperties);

        var propertyValue = _read.Get(entity);

        var text = ServiceBusMetadataSerializer.Serialize(propertyValue);
        if (!string.IsNullOrWhiteSpace(text))
            entityProperties.Add(
                _storageName,
                AzureTableSagaPropertyValidator.ValidateValue(text, _propertyName));
    }
}
