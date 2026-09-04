using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Used to read a header from a transport message
/// </summary>
public interface IHeaderProvider
{
    /// <summary>
    /// Gets all.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<KeyValuePair<string, object>> GetAll();

    /// <summary>
    /// Attempts to get header.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeader(string key, [NotNullWhen(true)] out object? value);
}
