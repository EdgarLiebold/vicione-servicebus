using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides a value type entity property converter implementation.
/// </summary>
/// <typeparam name="TEntity">The t entity type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class ValueTypeEntityPropertyConverter<TEntity, TProperty> :
    IEntityPropertyConverter<TEntity>
    where TEntity : class
    where TProperty : struct
{
    readonly string _name;
    readonly IReadProperty<TEntity, TProperty> _read;
    readonly IWriteProperty<TEntity, TProperty> _write;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    public ValueTypeEntityPropertyConverter(string name)
    {
        _name = name;
        _read = ReadPropertyCache<TEntity>.GetProperty<TProperty>(name);
        _write = WritePropertyCache<TEntity>.GetProperty<TProperty>(name);
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
            TProperty? propertyValue = ObjectDeserializer.Deserialize<TProperty>(entityProperty.ToString());

            if (propertyValue.HasValue)
                _write.Set(entity, propertyValue.Value);
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

        var text = ObjectDeserializer.Serialize(propertyValue);
        if (!string.IsNullOrWhiteSpace(text))
            entityProperties.Add(_name, text);
    }
}
