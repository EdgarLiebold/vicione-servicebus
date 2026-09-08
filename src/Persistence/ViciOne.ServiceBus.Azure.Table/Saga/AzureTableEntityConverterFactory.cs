using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Builds Azure Table converters for the readable and writable properties of a saga state type.</summary>
internal static class AzureTableEntityConverterFactory
{
    internal const string SagaPropertyPrefix = "Saga_";

    /// <summary>Creates a converter that isolates saga fields from Azure Table system fields and serializes unsupported native values.</summary>
    /// <typeparam name="T">The saga state type to convert.</typeparam>
    /// <returns>A converter for all public readable and writable properties of <typeparamref name="T"/>.</returns>
    public static IAzureTableEntityConverter<T> CreateConverter<T>()
        where T : class
    {
        List<IAzureTableSagaPropertyConverter<T>> converters = MessageTypeCache<T>.Properties.Where(x => x.CanRead && x.CanWrite)
            .Select(x => CreatePropertyConverter<T>(x))
            .ToList();

        return new AzureTableEntityConverter<T>(converters);
    }

    static IAzureTableSagaPropertyConverter<T> CreatePropertyConverter<T>(PropertyInfo propertyInfo)
        where T : class
    {
        if (AzureTablePropertyTypeConverter.IsSupported(propertyInfo.PropertyType))
        {
            return CreatePropertyConverter<T>(typeof(AzureTableSagaPropertyConverter<,>), propertyInfo);
        }

        if (propertyInfo.PropertyType.IsValueType)
        {
            return CreatePropertyConverter<T>(typeof(SerializedValueAzureTableSagaPropertyConverter<,>), propertyInfo);
        }

        return CreatePropertyConverter<T>(typeof(SerializedReferenceAzureTableSagaPropertyConverter<,>), propertyInfo);
    }

    static IAzureTableSagaPropertyConverter<T> CreatePropertyConverter<T>(Type openConverterType, PropertyInfo propertyInfo)
        where T : class
    {
        Type converterType = openConverterType.MakeGenericType(typeof(T), propertyInfo.PropertyType);
        string storageName = AzureTableSagaPropertyValidator.ValidateStorageName(
            $"{SagaPropertyPrefix}{propertyInfo.Name}",
            nameof(propertyInfo));
        return Activator.CreateInstance(converterType, propertyInfo.Name, storageName) as IAzureTableSagaPropertyConverter<T>
            ?? throw new InvalidOperationException(
                $"Unable to create Azure Table converter {converterType.FullName} for property {typeof(T).FullName}.{propertyInfo.Name}.");
    }
}
