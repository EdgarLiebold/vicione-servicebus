namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for group key provider.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public interface IGroupKeyProvider<in TMessage, TKey>
    where TMessage : class
{
    /// <summary>
    /// Attempts to get key.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="key">The key value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetKey(ConsumeContext<TMessage> context, out TKey key);
}
