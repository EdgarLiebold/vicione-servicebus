using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

public static class TransportSetHeaderAdapterExtensions
{
    static readonly ITransportSetHeaderAdapter<object> _adapter = new DictionaryTransportSetHeaderAdapter(new StringHeaderValueConverter());

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            adapter.Set(dictionary, new HeaderValue<string>(key, value!));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Uri? value)
    {
        if (value != null)
            adapter.Set(dictionary, new HeaderValue<string>(key, value.ToString()));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Guid? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        Guid? value, Func<Guid, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        int? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        int? value, Func<int, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        TimeSpan? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        TimeSpan? value, Func<TimeSpan, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        DateTimeOffset? value)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, ToString(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary, string key,
        DateTimeOffset? value, Func<DateTimeOffset, string> formatter)
    {
        if (value.HasValue)
            adapter.Set(dictionary, new HeaderValue<string>(key, formatter(value.Value)));
    }

    public static void Set<TValueType>(this ITransportSetHeaderAdapter<TValueType> adapter, IDictionary<string, TValueType> dictionary,
        IEnumerable<HeaderValue> headerValues)
    {
        foreach (var header in headerValues)
            adapter.Set(dictionary, header);
    }

    public static void Set(this IDictionary<string, object> dictionary, IEnumerable<HeaderValue> headerValues)
    {
        foreach (var header in headerValues)
            _adapter.Set(dictionary, header);
    }

    public static bool TryGetInt(this IDictionary<string, string> dictionary, string key, out int value)
    {
        if (dictionary.TryGetValue(key, out var text))
            return int.TryParse(text, out value);

        value = default;
        return false;
    }

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
