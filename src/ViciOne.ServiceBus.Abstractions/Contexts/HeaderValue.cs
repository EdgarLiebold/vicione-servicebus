using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents a header value value.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public readonly struct HeaderValue<T>
{
    /// <summary>
    /// Defines the key value.
    /// </summary>
    public readonly string Key;
    /// <summary>
    /// Defines the value value.
    /// </summary>
    public readonly T Value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public HeaderValue(string key, T value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Determines whether string value.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Determines whether simple value.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsSimpleValue([NotNullWhen(true)] out HeaderValue result)
    {
        return HeaderValue.IsValueSimpleValue(Key, Value, out result);
    }
}


/// <summary>
/// Represents a header value value.
/// </summary>
public readonly struct HeaderValue
{
    /// <summary>
    /// Defines the key value.
    /// </summary>
    public readonly string Key;
    /// <summary>
    /// Defines the value value.
    /// </summary>
    public readonly object Value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public HeaderValue(string key, object value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pair">The pair value.</param>
    public HeaderValue(KeyValuePair<string, object> pair)
    {
        Key = pair.Key;
        Value = pair.Value;
    }

    /// <summary>
    /// Determines whether string value.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStringValue([NotNullWhen(true)] out HeaderValue<string> result)
    {
        return IsValueStringValue(Key, Value, out result);
    }

    /// <summary>
    /// Determines whether simple value.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsSimpleValue([NotNullWhen(true)] out HeaderValue result)
    {
        return IsValueSimpleValue(Key, Value, out result);
    }

    /// <summary>
    /// Converts a value to <see cref="HeaderValue" />.
    /// </summary>
    /// <param name="headerValue">The header value value.</param>
    /// <returns>The result of the operation.</returns>
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
            case bool boolValue when boolValue:
                result = new HeaderValue<string>(key, bool.TrueString);
                return true;
            case Uri uri:
                result = new HeaderValue<string>(key, uri.ToString());
                return true;
            case IFormattable formatValue when formatValue.GetType().IsValueType:
                result = new HeaderValue<string>(key, formatValue.ToString()!);
                return true;
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
            case bool boolValue when boolValue:
                result = new HeaderValue(key, true);
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
