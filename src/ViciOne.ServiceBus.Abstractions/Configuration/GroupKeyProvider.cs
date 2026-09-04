using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a group key provider implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public class GroupKeyProvider<TMessage, TKey> :
    IGroupKeyProvider<TMessage, TKey>
    where TMessage : class
    where TKey : class
{
    readonly Func<ConsumeContext<TMessage>, TKey> _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public GroupKeyProvider(Func<ConsumeContext<TMessage>, TKey> provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Attempts to get key.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="key">The key value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetKey(ConsumeContext<TMessage> context, out TKey key)
    {
        key = _provider(context);

        return key != null;
    }
}
