using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides extension methods for transport set header adapter.</summary>
public static class TransportSetHeaderAdapterExtensions
{
    static readonly ITransportSetHeaderAdapter<object> _adapter = new DictionaryTransportSetHeaderAdapter(new StringHeaderValueConverter());

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            adapter.Set(dictionary, new HeaderValue<string>(key, value!));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Uri? value)
    {
        if (value != null)
            adapter.Set(dictionary, new HeaderValue<string>(key, value.ToString()));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Guid? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="formatter">The formatter.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Guid? value, Func<Guid, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        int? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="formatter">The formatter.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        int? value, Func<int, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        TimeSpan? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="formatter">The formatter.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        TimeSpan? value, Func<TimeSpan, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        DateTimeOffset? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="formatter">The formatter.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        DateTimeOffset? value, Func<DateTimeOffset, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TValueType">The value type type.</typeparam>
    /// <param name="adapter">The adapter.</param>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValues">The header values.</param>
    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary,
        IEnumerable<HeaderValue> headerValues)
    {
        foreach (var header in headerValues)
            adapter.Set(dictionary, header);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValues">The header values.</param>
    public static void Set(this IDictionary<string, object> dictionary, IEnumerable<HeaderValue> headerValues)
    {
        foreach (var header in headerValues)
            _adapter.Set(dictionary, header);
    }

    /// <summary>Attempts to get int.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetInt(this IDictionary<string, string> dictionary, string key, out int value)
    {
        if (dictionary.TryGetValue(key, out var text))
            return int.TryParse(text, out value);

        value = default;
        return false;
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValues">The header values.</param>
    public static void Set(this IDictionary<string, object> dictionary, params HeaderValue[] headerValues)
    {
        foreach (var header in headerValues)
            _adapter.Set(dictionary, header);
    }

    static string ToString(TimeSpan timeSpan)
    {
        return timeSpan.TotalMilliseconds.ToString("F0");
    }

    static string ToString(Guid guid)
    {
        return guid.ToString();
    }

    static string ToString(int value)
    {
        return value.ToString();
    }

    static string ToString(DateTimeOffset dateTime)
    {
        return dateTime.ToString("O");
    }
}
