using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;
/// <summary>A simple in-memory header collection for use with the in memory transport.</summary>
public class DictionarySendHeaderProvider :
    IHeaderProvider
{
    readonly SendHeaders _headers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="headers">The headers.</param>
    public DictionarySendHeaderProvider(SendHeaders headers)
    {
        _headers = headers;
    }

    /// <summary>Gets all.</summary>
    /// <returns>The all.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _headers.GetAll();
    }

    /// <summary>Attempts to get header.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        return _headers.TryGetHeader(key, out value);
    }
}
