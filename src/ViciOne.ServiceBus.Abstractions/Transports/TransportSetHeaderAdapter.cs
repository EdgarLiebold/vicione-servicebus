using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts transport set header between component contracts.</summary>
/// <typeparam name="TValueType">The value type type.</typeparam>
public class TransportSetHeaderAdapter<TValueType> :
    ITransportSetHeaderAdapter<TValueType>
{
    readonly IHeaderValueConverter<TValueType> _converter;
    readonly TransportHeaderOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    /// <param name="options">The options that control the operation.</param>
    public TransportSetHeaderAdapter(IHeaderValueConverter<TValueType> converter, TransportHeaderOptions options = TransportHeaderOptions.Default)
    {
        ArgumentNullException.ThrowIfNull(converter);

        _converter = converter;
        _options = options;
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
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

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
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
