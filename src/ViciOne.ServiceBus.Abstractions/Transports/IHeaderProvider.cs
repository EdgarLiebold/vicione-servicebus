// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;


    /// <summary>
    /// Used to read a header from a transport message
    /// </summary>
    public interface IHeaderProvider
    {
        IEnumerable<KeyValuePair<string, object>> GetAll();

        bool TryGetHeader(string key, [NotNullWhen(true)] out object? value);
    }
}
