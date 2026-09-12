using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Converts serialized metadata and message headers through an object deserializer.</summary>
public static class SerializerContextExtensions
{
    /// <summary>Gets and deserializes a reference value by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The read-only metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IReadOnlyDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets and deserializes a nullable value type by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The read-only metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IReadOnlyDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets and deserializes a reference value by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The mutable metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets and deserializes a nullable value type by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The mutable metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets and deserializes a reference value from a header provider.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored header.</param>
    /// <param name="headers">The header provider to search.</param>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">The value returned when the header is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IHeaderProvider headers, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return headers.TryGetHeader(key, out var value) ? context.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Gets and deserializes a nullable value type from a header provider.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored header.</param>
    /// <param name="headers">The header provider to search.</param>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">The value returned when the header is absent or conversion produces no value.</param>
    /// <returns>The converted value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IHeaderProvider headers, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return headers.TryGetHeader(key, out var value) ? context.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Tries to get and deserialize a reference value by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The converted value when conversion succeeds; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the key exists and conversion produces a value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key,
        [NotNullWhen(true)] out T? value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var obj) && !dictionary.TryGetValueCamelCase(key, out obj))
        {
            value = null;
            return false;
        }

        value = context.DeserializeObject<T>(obj);
        return value != null;
    }

    /// <summary>Serializes the non-null entries with valid keys in a metadata sequence.</summary>
    /// <param name="deserializer">The serializer used to encode the metadata dictionary.</param>
    /// <param name="values">The metadata entries to serialize.</param>
    /// <returns>The serialized case-insensitive dictionary, or <see langword="null" /> when no non-null entries remain.</returns>
    public static string? SerializeDictionary(this IObjectDeserializer deserializer, IEnumerable<KeyValuePair<string, object>> values)
    {
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(values);

        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> pair in values)
        {
            if (pair.Value == null)
                continue;

            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key, nameof(values));
            dictionary[pair.Key] = pair.Value;
        }

        return dictionary.Count == 0
            ? null
            : deserializer.SerializeObject(dictionary).GetRequiredTransportText();
    }

    /// <summary>Deserializes a sequence of metadata entries into a case-insensitive dictionary.</summary>
    /// <typeparam name="TValue">The metadata value type.</typeparam>
    /// <param name="deserializer">The deserializer used to decode the metadata entries.</param>
    /// <param name="text">The serialized metadata sequence.</param>
    /// <returns>The populated dictionary, or <see langword="null" /> when the input is empty or contains no entries.</returns>
    public static Dictionary<string, TValue>? DeserializeDictionary<TValue>(this IObjectDeserializer deserializer, string? text)
    {
        ArgumentNullException.ThrowIfNull(deserializer);

        if (!string.IsNullOrWhiteSpace(text))
        {
            List<KeyValuePair<string, TValue>>? headers = deserializer.DeserializeObject<IEnumerable<KeyValuePair<string, TValue>>>(text)?.ToList();
            if (headers != null && headers.Count > 0)
            {
                var dictionary = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, TValue> pair in headers)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key, nameof(text));
                    dictionary[pair.Key] = pair.Value;
                }

                return dictionary;
            }
        }

        return null;
    }

    /// <summary>Tries to get and deserialize a nullable value type by its exact or camel-case key.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The deserializer used to convert the stored value.</param>
    /// <param name="dictionary">The metadata dictionary to search.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The converted value when conversion succeeds; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the key exists and conversion produces a value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key,
        [NotNullWhen(true)] out T? value)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!dictionary.TryGetValue(key, out var obj) && !dictionary.TryGetValueCamelCase(key, out obj))
        {
            value = null;
            return false;
        }

        value = context.DeserializeObject<T>(obj);
        return value != null;
    }

    /// <summary>Tries to deserialize a reference-valued consume header.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The consume context whose headers and serializer are used.</param>
    /// <param name="key">The header key.</param>
    /// <param name="value">The converted header when conversion succeeds; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the header exists and conversion produces a value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this ConsumeContext context, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = null;
            return false;
        }

        value = context.SerializerContext.DeserializeObject<T>(headerValue);
        return value != null;
    }

    /// <summary>Tries to deserialize a nullable value-type consume header.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The consume context whose headers and serializer are used.</param>
    /// <param name="key">The header key.</param>
    /// <param name="value">The converted header when conversion succeeds; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the header exists and conversion produces a value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this ConsumeContext context, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = null;
            return false;
        }

        value = context.SerializerContext.DeserializeObject<T>(headerValue);
        return value != null;
    }

    /// <summary>Tries to read a send header already stored as the requested reference type.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The send context whose headers are read.</param>
    /// <param name="key">The header key.</param>
    /// <param name="value">The typed header when present; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the header is stored as the requested type; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this SendContext context, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = headerValue as T;
            return value != null;
        }

        value = null;
        return false;
    }

    /// <summary>Tries to read a send header already stored as the requested value type.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The send context whose headers are read.</param>
    /// <param name="key">The header key.</param>
    /// <param name="value">The typed header when present; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the header is stored as the requested type; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this SendContext context, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = headerValue as T?;
            return value != null;
        }

        value = null;
        return false;
    }

    /// <summary>Gets a consume header as text, deserializing non-text values when necessary.</summary>
    /// <param name="context">The consume context whose headers and serializer are used.</param>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">The value returned when the header is absent or conversion produces no value.</param>
    /// <returns>The text value, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static string? GetHeader(this ConsumeContext context, string key, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        if (headerValue is string text)
            return text;

        return context.SerializerContext.DeserializeObject<string>(headerValue) ?? defaultValue;
    }

    /// <summary>Gets and deserializes a reference-valued consume header.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The consume context whose headers and serializer are used.</param>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">The value returned when the header is absent or conversion produces no value.</param>
    /// <returns>The converted header, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetHeader<T>(this ConsumeContext context, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        return context.SerializerContext.DeserializeObject<T>(headerValue) ?? defaultValue;
    }

    /// <summary>Gets and deserializes a nullable value-type consume header.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The consume context whose headers and serializer are used.</param>
    /// <param name="key">The header key.</param>
    /// <param name="defaultValue">The value returned when the header is absent or conversion produces no value.</param>
    /// <returns>The converted header, or <paramref name="defaultValue" /> when no value is available.</returns>
    public static T? GetHeader<T>(this ConsumeContext context, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        return context.SerializerContext.DeserializeObject<T>(headerValue) ?? defaultValue;
    }

    /// <summary>Converts an object into the consume context serializer's property dictionary.</summary>
    /// <typeparam name="T">The source object type.</typeparam>
    /// <param name="context">The consume context whose serializer is used.</param>
    /// <param name="value">The object to convert.</param>
    /// <returns>A dictionary containing the object's serialized properties.</returns>
    public static Dictionary<string, object> ToDictionary<T>(this ConsumeContext context, T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(value);

        return context.SerializerContext.ToDictionary(value);
    }
}
