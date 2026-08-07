// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Caching
{
    /// <summary>
    /// Returns the key for a value
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public delegate TKey KeyProvider<out TKey, in TValue>(TValue value);
}
