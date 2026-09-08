using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Converts saga state to and from isolated Azure Table property dictionaries.</summary>
/// <typeparam name="T">The saga state materialized by the converter.</typeparam>
internal sealed class AzureTableEntityConverter<T> :
    IAzureTableEntityConverter<T>
    where T : class
{
    readonly IAzureTableSagaPropertyConverter<T>[] _converters;

    /// <summary>Creates a converter from the ordered property converters for <typeparamref name="T"/>.</summary>
    /// <param name="converters">The converters that read and write individual instance properties.</param>
    public AzureTableEntityConverter(IReadOnlyList<IAzureTableSagaPropertyConverter<T>> converters)
    {
        ArgumentNullException.ThrowIfNull(converters);
        if (typeof(T).IsAbstract || typeof(T).GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"Azure Table saga type '{typeof(T).FullName}' must be concrete and expose a public parameterless constructor.");
        }

        if (converters.Count > AzureTableStorageLimits.MaximumCustomPropertyCount)
        {
            throw new ArgumentException(
                $"An Azure Table saga can contain at most {AzureTableStorageLimits.MaximumCustomPropertyCount} persisted properties.",
                nameof(converters));
        }

        var storageNames = new HashSet<string>(StringComparer.Ordinal);
        var validatedConverters = new IAzureTableSagaPropertyConverter<T>[converters.Count];
        for (var i = 0; i < converters.Count; i++)
        {
            IAzureTableSagaPropertyConverter<T> converter = converters[i]
                ?? throw new ArgumentException(
                    "The Azure Table saga property converter collection cannot contain null entries.",
                    nameof(converters));

            if (!storageNames.Add(converter.StorageName))
            {
                throw new ArgumentException(
                    $"Azure Table saga property name '{converter.StorageName}' is configured more than once.",
                    nameof(converters));
            }

            validatedConverters[i] = converter;
        }

        _converters = validatedConverters;
    }

    /// <summary>Projects all configured properties of an instance into an Azure Table property dictionary.</summary>
    /// <param name="entity">The instance to project.</param>
    /// <returns>The isolated Azure Table-compatible property names and values.</returns>
    public IDictionary<string, object> GetDictionary(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityProperties = new Dictionary<string, object>(StringComparer.Ordinal);
        for (var i = 0; i < _converters.Length; i++)
            _converters[i].FromEntity(entity, entityProperties);

        return entityProperties;
    }

    /// <summary>Creates an instance and populates it from persisted Azure Table properties.</summary>
    /// <param name="entityProperties">The persisted property names and values.</param>
    /// <returns>The materialized instance.</returns>
    public T GetObject(IDictionary<string, object> entityProperties)
    {
        ArgumentNullException.ThrowIfNull(entityProperties);

        var entity = Activator.CreateInstance<T>()
            ?? throw new InvalidOperationException($"Unable to create Azure Table entity type {typeof(T).FullName}.");

        for (var i = 0; i < _converters.Length; i++)
            _converters[i].ToEntity(entity, entityProperties);

        return entity;
    }
}
