using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts dictionary transport set header between component contracts.</summary>
public class DictionaryTransportSetHeaderAdapter :
    ITransportSetHeaderAdapter<object>
{
    readonly IHeaderValueConverter _converter;
    readonly TransportHeaderOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    /// <param name="options">The options that control the operation.</param>
    public DictionaryTransportSetHeaderAdapter(IHeaderValueConverter converter, TransportHeaderOptions options = TransportHeaderOptions.Default)
    {
        _converter = converter;
        _options = options;
    }

    /// <summary>Gets or sets the max header length.</summary>
    public int? MaxHeaderLength { get; set; }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
    public void Set(IDictionary<string, object> dictionary, in HeaderValue headerValue)
    {
        switch (headerValue.Value)
        {
            case null:
                if (dictionary.ContainsKey(headerValue.Key))
                    dictionary.Remove(headerValue.Key);
                break;

            default:
                if (IsHeaderIncluded(headerValue.Key) && _converter.TryConvert(headerValue, out var result))
                    dictionary[result.Key] = TrimHeaderIfLengthExceedsLimit(result.Value);

                break;
        }
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="headerValue">The header value to convert or store.</param>
    public void Set<T>(IDictionary<string, object> dictionary, in HeaderValue<T> headerValue)
    {
        switch (headerValue.Value)
        {
            case null:
            case string s when string.IsNullOrWhiteSpace(s):
                if (dictionary.ContainsKey(headerValue.Key))
                    dictionary.Remove(headerValue.Key);
                break;

            default:
                if (IsHeaderIncluded(headerValue.Key) && _converter.TryConvert(headerValue, out var result))
                    dictionary[result.Key] = TrimHeaderIfLengthExceedsLimit(result.Value);
                break;
        }
    }

    object TrimHeaderIfLengthExceedsLimit(object value)
    {
        if (MaxHeaderLength.HasValue && value is string stringValue && stringValue.Length > MaxHeaderLength.Value)
            value = stringValue.Substring(0, MaxHeaderLength.Value);

        return value;
    }

    bool IsHeaderIncluded(string key)
    {
        if (key.StartsWith(MessageHeaders.Host.Prefix, StringComparison.Ordinal))
            return _options.HasFlag(TransportHeaderOptions.IncludeHost);

        if (key.StartsWith(MessageHeaders.FaultPrefix, StringComparison.Ordinal))
        {
            if (_options.HasFlag(TransportHeaderOptions.IncludeFaultDetail))
                return true;

            return _options.HasFlag(TransportHeaderOptions.IncludeFaultMessage) && key.Equals(MessageHeaders.FaultMessage);
        }

        return true;
    }
}
