using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Used to read a header from a transport message
/// </summary>
public interface IHeaderProvider
{
    IEnumerable<KeyValuePair<string, object>> GetAll();

    bool TryGetHeader(string key, [NotNullWhen(true)] out object? value);
}
