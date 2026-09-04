using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an entity converter factory implementation.
/// </summary>
public static class EntityConverterFactory
{
    /// <summary>
    /// Creates converter.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IEntityConverter<T> CreateConverter<T>()
        where T : class
    {
        List<IEntityPropertyConverter<T>> converters = MessageTypeCache<T>.Properties.Where(x => x.CanRead && x.CanWrite)
            .Select(x => CreatePropertyConverter<T>(x))
            .ToList();

        return new EntityConverter<T>(converters);
    }

    static IEntityPropertyConverter<T> CreatePropertyConverter<T>(PropertyInfo propertyInfo)
        where T : class
    {
        if (EntityPropertyTypeConverter.IsSupported(propertyInfo.PropertyType))
        {
            return CreatePropertyConverter<T>(typeof(EntityPropertyConverter<,>), propertyInfo);
        }

        if (propertyInfo.PropertyType.IsValueType)
        {
            return CreatePropertyConverter<T>(typeof(ValueTypeEntityPropertyConverter<,>), propertyInfo);
        }

        return CreatePropertyConverter<T>(typeof(ObjectEntityPropertyConverter<,>), propertyInfo);
    }

    static IEntityPropertyConverter<T> CreatePropertyConverter<T>(Type openConverterType, PropertyInfo propertyInfo)
        where T : class
    {
        Type converterType = openConverterType.MakeGenericType(typeof(T), propertyInfo.PropertyType);
        return Activator.CreateInstance(converterType, propertyInfo.Name) as IEntityPropertyConverter<T>
            ?? throw new InvalidOperationException(
                $"Unable to create Azure Table converter {converterType.FullName} for property {typeof(T).FullName}.{propertyInfo.Name}.");
    }
}
