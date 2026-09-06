using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Builds Azure Table converters for the readable and writable properties of a reference type.</summary>
public static class EntityConverterFactory
{
    /// <summary>Creates a converter using native Azure property conversion where possible and serialized fallback conversion otherwise.</summary>
    /// <typeparam name="T">The reference type to convert.</typeparam>
    /// <returns>A converter for all public readable and writable properties of <typeparamref name="T"/>.</returns>
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
