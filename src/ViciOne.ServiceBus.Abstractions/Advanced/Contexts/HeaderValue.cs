using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Associates a transport-header name with a typed non-null value.</summary>
/// <typeparam name="TValue">The header-value type.</typeparam>
public readonly struct HeaderValue<TValue>
{
    /// <summary>Creates a typed transport-header value.</summary>
    /// <param name="key">The non-empty transport-header name.</param>
    /// <param name="value">The non-null transport-header value.</param>
    public HeaderValue(string key, TValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        Key = key;
        Value = value;
    }

    /// <summary>Gets the transport-header name.</summary>
    public string Key { get; }

    /// <summary>Gets the typed transport-header value.</summary>
    public TValue Value { get; }

    /// <summary>Attempts to represent the value as culture-invariant text.</summary>
    /// <param name="result">Receives the string-valued header when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the value has a supported text representation; otherwise, <see langword="false" />.</returns>
    public bool IsStringValue([NotNullWhen(true)] out HeaderValue<string> result)
    {
        switch (this)
        {
            case HeaderValue<string> resultValue:
                result = resultValue;
                return true;
            default:
                return HeaderValue.IsValueStringValue(Key, Value, out result);
        }
    }

    /// <summary>Attempts to preserve the value as a transport-safe scalar.</summary>
    /// <param name="result">Receives the scalar-valued header when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the value is a supported scalar; otherwise, <see langword="false" />.</returns>
    public bool IsSimpleValue([NotNullWhen(true)] out HeaderValue result)
    {
        return HeaderValue.IsValueSimpleValue(Key, Value, out result);
    }
}


/// <summary>Associates a transport-header name with a non-null value.</summary>
public readonly struct HeaderValue
{
    /// <summary>Creates a transport-header value.</summary>
    /// <param name="key">The non-empty transport-header name.</param>
    /// <param name="value">The non-null transport-header value.</param>
    public HeaderValue(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        Key = key;
        Value = value;
    }

    /// <summary>Creates a transport-header value from a dictionary entry.</summary>
    /// <param name="pair">The transport-header name and value.</param>
    public HeaderValue(KeyValuePair<string, object> pair)
        : this(pair.Key, pair.Value)
    {
    }

    /// <summary>Gets the transport-header name.</summary>
    public string Key { get; }

    /// <summary>Gets the transport-header value.</summary>
    public object Value { get; }

    /// <summary>Attempts to represent the value as culture-invariant text.</summary>
    /// <param name="result">Receives the string-valued header when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the value has a supported text representation; otherwise, <see langword="false" />.</returns>
    public bool IsStringValue([NotNullWhen(true)] out HeaderValue<string> result)
    {
        return IsValueStringValue(Key, Value, out result);
    }

    /// <summary>Attempts to preserve the value as a transport-safe scalar.</summary>
    /// <param name="result">Receives the scalar-valued header when conversion succeeds.</param>
    /// <returns><see langword="true" /> when the value is a supported scalar; otherwise, <see langword="false" />.</returns>
    public bool IsSimpleValue([NotNullWhen(true)] out HeaderValue result)
    {
        return IsValueSimpleValue(Key, Value, out result);
    }

    /// <summary>Converts a typed string header to its non-generic representation.</summary>
    /// <param name="headerValue">The typed string header.</param>
    /// <returns>The equivalent non-generic header value.</returns>
    public static implicit operator HeaderValue(HeaderValue<string> headerValue)
    {
        return new HeaderValue(headerValue.Key, headerValue.Value);
    }

    internal static bool IsValueStringValue(string key, object? value, [NotNullWhen(true)] out HeaderValue<string> result)
    {
        switch (value)
        {
            case null:
                result = default;
                return false;
            case string stringValue:
                result = new HeaderValue<string>(key, stringValue);
                return true;
            case bool boolValue:
                result = new HeaderValue<string>(key, boolValue.ToString());
                return true;
            case Uri uri:
                result = new HeaderValue<string>(key, uri.ToString());
                return true;
            case IFormattable formatValue when formatValue.GetType().IsValueType:
                string? text = formatValue.ToString(null, CultureInfo.InvariantCulture);
                if (text is not null)
                {
                    result = new HeaderValue<string>(key, text);
                    return true;
                }

                result = default;
                return false;
            default:
                result = default;
                return false;
        }
    }

    internal static bool IsValueSimpleValue(string key, object? value, [NotNullWhen(true)] out HeaderValue result)
    {
        switch (value)
        {
            case null:
                result = default;
                return false;
            case string stringValue:
                result = new HeaderValue<string>(key, stringValue);
                return true;
            case bool boolValue:
                result = new HeaderValue(key, boolValue);
                return true;
            case Uri uri:
                result = new HeaderValue<string>(key, uri.ToString());
                return true;
            case IFormattable formatValue when formatValue.GetType().IsValueType:
                result = new HeaderValue(key, value);
                return true;
            default:
                result = default;
                return false;
        }
    }
}
