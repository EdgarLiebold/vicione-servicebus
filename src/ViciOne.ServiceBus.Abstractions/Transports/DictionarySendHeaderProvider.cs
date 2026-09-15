using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes outgoing message headers through the transport header-provider contract.</summary>
public class DictionarySendHeaderProvider :
    IHeaderProvider
{
    readonly SendHeaders _headers;

    /// <summary>Creates a provider backed by the supplied outgoing headers.</summary>
    /// <param name="headers">The header collection to read. Changes to the collection remain visible through this provider.</param>
    public DictionarySendHeaderProvider(SendHeaders headers)
    {
        _headers = headers;
    }

    /// <summary>Enumerates the headers exposed by the backing collection.</summary>
    /// <returns>The backing collection's header entries.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        return _headers.GetAll();
    }

    /// <summary>Tries to read a header from the backing collection.</summary>
    /// <param name="key">The header name to look up.</param>
    /// <param name="value">The header value when the lookup succeeds; otherwise, the backing collection's absent-value result.</param>
    /// <returns><see langword="true" /> when the backing collection supplies the header; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        return _headers.TryGetHeader(key, out value);
    }
}
