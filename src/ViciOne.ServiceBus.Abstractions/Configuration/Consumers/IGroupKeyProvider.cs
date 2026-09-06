namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides group key services.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface IGroupKeyProvider<in TMessage, TKey>
    where TMessage : class
{
    /// <summary>Attempts to get key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">Receives the key produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetKey(ConsumeContext<TMessage> context, out TKey key);
}
