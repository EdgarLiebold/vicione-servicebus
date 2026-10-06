using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Serialization.Json.Converters;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Prepares header conversion without hiding source serialization or user converter failures.</summary>
internal static class HeaderValueConversion
{
    internal static object? PrepareHeaderValue<T>(object value, out bool incompatible, out bool useBuiltInFallback,
        bool preserveEncodedNull = false)
    {
        incompatible = false;
        useBuiltInFallback = false;
        if (value is T)
            return value;

        JsonElement element;
        switch (value)
        {
            case string text when string.IsNullOrWhiteSpace(text):
                return null;
            case string text when TypeConverterCache.TryGetTypeConverter(out ITypeConverter<T, string>? converter)
                && converter.TryConvert(text, out var result):
                return result;
            case string text:
                element = JsonSerializer.Deserialize<JsonElement>(text, ServiceBusMetadataJson.Options);
                break;
            case JsonElement jsonElement:
                element = jsonElement;
                break;
            default:
                element = JsonSerializer.SerializeToElement(value, ServiceBusMetadataJson.Options);
                break;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            if (!preserveEncodedNull || value is not string && value is not JsonElement)
                return null;

            useBuiltInFallback = IsBuiltInHeaderTarget(typeof(T), ServiceBusMetadataJson.Options.GetTypeInfo(typeof(T)));
            return element;
        }
        if (element.ValueKind == JsonValueKind.Undefined)
            return element;

        JsonTypeInfo typeInfo = ServiceBusMetadataJson.Options.GetTypeInfo(typeof(T));
        incompatible = typeInfo.Kind switch
        {
            JsonTypeInfoKind.Object or JsonTypeInfoKind.Dictionary => element.ValueKind != JsonValueKind.Object,
            JsonTypeInfoKind.Enumerable => element.ValueKind != JsonValueKind.Array,
            _ => false,
        };
        if (GetOwnedDictionaryValueType(typeInfo.Converter.GetType(), out bool allowsArray) != null)
            incompatible = element.ValueKind != JsonValueKind.Object && !(allowsArray && element.ValueKind == JsonValueKind.Array);
        useBuiltInFallback = IsBuiltInHeaderTarget(typeof(T), typeInfo);
        return element;
    }

    static Type? GetOwnedDictionaryValueType(Type converterType, out bool allowsArray)
    {
        allowsArray = false;
        if (!converterType.IsGenericType)
            return null;

        Type definition = converterType.GetGenericTypeDefinition();
        if (definition == typeof(CaseInsensitiveDictionaryStringObjectJsonConverter<>))
        {
            allowsArray = true;
            return typeof(object);
        }
        return definition == typeof(CaseInsensitiveDictionaryJsonConverter<,>)
            || definition == typeof(UriDictionarySystemTextJsonConverter<,>)
            ? converterType.GetGenericArguments()[1]
            : null;
    }

    static bool IsBuiltInHeaderTarget(Type type, JsonTypeInfo typeInfo)
    {
        Type? dictionaryValueType = GetOwnedDictionaryValueType(typeInfo.Converter.GetType(), out bool allowsArray);
        if (dictionaryValueType != null)
            return allowsArray && dictionaryValueType == typeof(object)
                || IsBuiltInHeaderTarget(dictionaryValueType, ServiceBusMetadataJson.Options.GetTypeInfo(dictionaryValueType));
        if (type == typeof(decimal) && typeInfo.Converter is StringDecimalJsonConverter)
            return true;
        if (typeInfo.Converter.GetType().Assembly != typeof(JsonSerializer).Assembly)
            return false;
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return IsBuiltInHeaderTarget(underlyingType, ServiceBusMetadataJson.Options.GetTypeInfo(underlyingType));

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan)
            || type == typeof(Uri) || type == typeof(Version) || type == typeof(Half)
            || type == typeof(Int128) || type == typeof(UInt128) || type == typeof(byte[]))
            return true;

        Type? elementType = type.IsArray && type.GetArrayRank() == 1 ? type.GetElementType() : null;
        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            if (definition == typeof(List<>) || definition == typeof(IList<>)
                || definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
                || definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
                elementType = type.GetGenericArguments()[0];
        }

        return elementType != null
            && IsBuiltInHeaderTarget(elementType, ServiceBusMetadataJson.Options.GetTypeInfo(elementType));
    }
}
