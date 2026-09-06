using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;

/// <summary>A simple in-memory header collection for use with the in memory transport.</summary>
public class DictionaryHeaderProvider :
    IHeaderProvider
{
    readonly IDictionary<string, object> _headers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="headers">The headers.</param>
    public DictionaryHeaderProvider(IDictionary<string, object>? headers = default)
    {
        _headers = headers ?? new Dictionary<string, object>();
    }

    /// <summary>Gets all.</summary>
    /// <returns>The all.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _headers;
    }

    /// <summary>Attempts to get header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        return _headers.TryGetValue(key, out value);
    }
}
