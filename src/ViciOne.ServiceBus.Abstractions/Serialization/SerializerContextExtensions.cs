using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Provides extension methods for serializer context.</summary>
public static class SerializerContextExtensions
{
    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IReadOnlyDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : class
    {
        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IReadOnlyDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : struct
    {
        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : class
    {
        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key, T? defaultValue = null)
        where T : struct
    {
        if (!dictionary.TryGetValue(key, out var value) && !dictionary.TryGetValueCamelCase(key, out value))
            return defaultValue;

        return context.DeserializeObject(value, defaultValue);
    }

    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IHeaderProvider dictionary, string key, T? defaultValue = null)
        where T : class
    {
        return dictionary.TryGetHeader(key, out var value) ? context.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Gets value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The value.</returns>
    public static T? GetValue<T>(this IObjectDeserializer context, IHeaderProvider dictionary, string key, T? defaultValue = null)
        where T : struct
    {
        return dictionary.TryGetHeader(key, out var value) ? context.DeserializeObject(value, defaultValue) : defaultValue;
    }

    /// <summary>Attempts to get value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key,
        [NotNullWhen(true)] out T? value)
        where T : class
    {
        if (!dictionary.TryGetValue(key, out var obj) && !dictionary.TryGetValueCamelCase(key, out obj))
        {
            value = null;
            return false;
        }

        value = context.DeserializeObject<T>(obj);
        return value != null;
    }

    /// <summary>Serializes dictionary.</summary>
    /// <param name="deserializer">The deserializer.</param>
    /// <param name="values">The values.</param>
    /// <returns>The serialized dictionary.</returns>
    public static string? SerializeDictionary(this IObjectDeserializer deserializer, IEnumerable<KeyValuePair<string, object>> values)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> pair in values)
        {
            if (pair.Value != null)
                dictionary[pair.Key] = pair.Value;
        }

        return dictionary.Count == 0
            ? null
            : deserializer.SerializeObject(dictionary).GetString();
    }

    /// <summary>Deserializes dictionary.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="deserializer">The deserializer.</param>
    /// <param name="text">The text.</param>
    /// <returns>The deserialized dictionary.</returns>
    public static Dictionary<string, TValue>? DeserializeDictionary<TValue>(this IObjectDeserializer deserializer, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            List<KeyValuePair<string, TValue>>? headers = deserializer.DeserializeObject<IEnumerable<KeyValuePair<string, TValue>>>(text)?.ToList();
            if (headers != null && headers.Count > 0)
            {
                var dictionary = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, TValue> x in headers)
                    dictionary.Add(x.Key, x.Value);

                return dictionary;
            }
        }

        return null;
    }

    /// <summary>Attempts to get value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IObjectDeserializer context, IDictionary<string, object> dictionary, string key,
        [NotNullWhen(true)] out T? value)
        where T : struct
    {
        if (!dictionary.TryGetValue(key, out var obj) && !dictionary.TryGetValueCamelCase(key, out obj))
        {
            value = null;
            return false;
        }

        value = context.DeserializeObject<T>(obj);
        return value != null;
    }

    /// <summary>Attempts to get header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this ConsumeContext context, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        if (!context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = null;
            return false;
        }

        value = context.SerializerContext.DeserializeObject<T>(headerValue);
        return value != null;
    }

    /// <summary>Attempts to get header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this ConsumeContext context, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        if (!context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = null;
            return false;
        }

        value = context.SerializerContext.DeserializeObject<T>(headerValue);
        return value != null;
    }

    /// <summary>Attempts to get header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this SendContext context, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        if (context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = headerValue as T;
            return value != null;
        }

        value = null;
        return false;
    }

    /// <summary>Attempts to get header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetHeader<T>(this SendContext context, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        if (context.Headers.TryGetHeader(key, out var headerValue))
        {
            value = headerValue as T?;
            return value != null;
        }

        value = null;
        return false;
    }

    /// <summary>Gets header.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The header.</returns>
    public static string? GetHeader(this ConsumeContext context, string key, string? defaultValue = null)
    {
        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        if (headerValue is string text)
            return text;

        return context.SerializerContext.DeserializeObject<string>(headerValue) ?? defaultValue;
    }

    /// <summary>Gets header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The header.</returns>
    public static T? GetHeader<T>(this ConsumeContext context, string key, T? defaultValue = null)
        where T : class
    {
        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        return context.SerializerContext.DeserializeObject<T>(headerValue) ?? defaultValue;
    }

    /// <summary>Gets header.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The header.</returns>
    public static T? GetHeader<T>(this ConsumeContext context, string key, T? defaultValue = null)
        where T : struct
    {
        if (!context.Headers.TryGetHeader(key, out var headerValue))
            return defaultValue;

        return context.SerializerContext.DeserializeObject<T>(headerValue) ?? defaultValue;
    }

    /// <summary>Converts this value to dictionary.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The converted dictionary.</returns>
    public static Dictionary<string, object> ToDictionary<T>(this ConsumeContext context, T value)
        where T : class
    {
        return context.SerializerContext.ToDictionary(value);
    }
}
