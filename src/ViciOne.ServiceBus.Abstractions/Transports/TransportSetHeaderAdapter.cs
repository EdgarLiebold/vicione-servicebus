using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a transport set header adapter implementation.
/// </summary>
/// <typeparam name="TValueType">The t value type type.</typeparam>
public class TransportSetHeaderAdapter<TValueType> :
    ITransportSetHeaderAdapter<TValueType>
{
    readonly IHeaderValueConverter<TValueType> _converter;
    readonly TransportHeaderOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    /// <param name="options">The options value.</param>
    public TransportSetHeaderAdapter(IHeaderValueConverter<TValueType> converter, TransportHeaderOptions options = TransportHeaderOptions.Default)
    {
        _converter = converter;
        _options = options;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="dictionary">The dictionary value.</param>
    /// <param name="headerValue">The header value value.</param>
    public void Set(IDictionary<string, TValueType> dictionary, in HeaderValue headerValue)
    {
        switch (headerValue.Value)
        {
            case null:
                if (dictionary.ContainsKey(headerValue.Key))
                    dictionary.Remove(headerValue.Key);
                break;

            default:
                if (IsHeaderIncluded(headerValue.Key) && _converter.TryConvert(headerValue, out HeaderValue<TValueType> result))
                    dictionary[result.Key] = result.Value;
                break;
        }
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="dictionary">The dictionary value.</param>
    /// <param name="headerValue">The header value value.</param>
    public void Set<T>(IDictionary<string, TValueType> dictionary, in HeaderValue<T> headerValue)
    {
        switch (headerValue.Value)
        {
            case null:
            case string s when string.IsNullOrWhiteSpace(s):
                if (dictionary.ContainsKey(headerValue.Key))
                    dictionary.Remove(headerValue.Key);
                break;

            default:
                if (IsHeaderIncluded(headerValue.Key) && _converter.TryConvert(headerValue, out HeaderValue<TValueType> result))
                    dictionary[result.Key] = result.Value;
                break;
        }
    }

    bool IsHeaderIncluded(string key)
    {
        if (key.StartsWith(MessageHeaders.Host.Prefix, StringComparison.Ordinal))
            return _options.HasFlag(TransportHeaderOptions.IncludeHost);

        if (key.Equals(MessageHeaders.FaultInputAddress))
            return true;

        if (key.StartsWith(MessageHeaders.FaultPrefix, StringComparison.Ordinal))
        {
            if (_options.HasFlag(TransportHeaderOptions.IncludeFaultDetail))
                return true;

            return _options.HasFlag(TransportHeaderOptions.IncludeFaultMessage) && key.Equals(MessageHeaders.FaultMessage);
        }

        return true;
    }
}
