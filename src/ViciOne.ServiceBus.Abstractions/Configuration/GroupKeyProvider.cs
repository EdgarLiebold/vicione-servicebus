using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides group key services.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public class GroupKeyProvider<TMessage, TKey> :
    IGroupKeyProvider<TMessage, TKey>
    where TMessage : class
    where TKey : class
{
    readonly Func<ConsumeContext<TMessage>, TKey> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public GroupKeyProvider(Func<ConsumeContext<TMessage>, TKey> provider)
    {
        _provider = provider;
    }

    /// <summary>Attempts to get key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">Receives the key produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetKey(ConsumeContext<TMessage> context, out TKey key)
    {
        key = _provider(context);

        return key != null;
    }
}
