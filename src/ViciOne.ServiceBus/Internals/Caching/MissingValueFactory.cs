// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System.Threading.Tasks;


    /// <summary>
    /// Creates the value if it is not found in the index
    /// </summary>
    /// <param name="key">The missing key</param>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    public delegate Task<TValue> MissingValueFactory<in TKey, TValue>(TKey key);
}
