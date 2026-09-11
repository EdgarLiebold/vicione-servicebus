using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides read-only access to the raw headers carried by a native transport message.</summary>
public interface IHeaderProvider
{
    /// <summary>Enumerates every header name and its unconverted transport value.</summary>
    /// <returns>The raw transport headers.</returns>
    IEnumerable<KeyValuePair<string, object>> GetAll();

    /// <summary>Tries to obtain the raw transport value associated with a header name.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The raw value when the header exists.</param>
    /// <returns><see langword="true" /> when the header exists; otherwise, <see langword="false" />.</returns>
    bool TryGetHeader(string key, [NotNullWhen(true)] out object? value);
}
