// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;


    /// <summary>
    /// A simple in-memory header collection for use with the in memory transport
    /// </summary>
    public class DictionaryHeaderProvider :
        IHeaderProvider
    {
        readonly IDictionary<string, object> _headers;

        public DictionaryHeaderProvider(IDictionary<string, object>? headers = default)
        {
            _headers = headers ?? new Dictionary<string, object>();
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            return _headers;
        }

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            return _headers.TryGetValue(key, out value);
        }
    }
}
