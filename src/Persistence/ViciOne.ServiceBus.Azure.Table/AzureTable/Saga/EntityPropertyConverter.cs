namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using System.Collections.Generic;
    using Initializers;
    using Internals;


    public class EntityPropertyConverter<TEntity, TProperty> :
        IEntityPropertyConverter<TEntity>
        where TEntity : class
    {
        readonly ITypeConverter<object, TProperty> _fromEntity;
        readonly string _name;
        readonly IReadProperty<TEntity, TProperty> _read;
        readonly ITypeConverter<TProperty, object> _toEntity;
        readonly IWriteProperty<TEntity, TProperty> _write;

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

        public void FromEntity(TEntity entity, IDictionary<string, object> entityProperties)
        {
            var propertyValue = _read.Get(entity);
            if (propertyValue is null)
                return;

            if (_fromEntity.TryConvert(propertyValue, out var entityProperty))
            {
                entityProperties.Add(_name, entityProperty);
                return;
            }

            throw new InvalidOperationException(
                $"Property '{_name}' of type '{typeof(TProperty)}' cannot be converted to an Azure Table value.");
        }
    }
}
