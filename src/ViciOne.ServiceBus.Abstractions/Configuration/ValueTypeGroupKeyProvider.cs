using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a value type group key provider implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public class ValueTypeGroupKeyProvider<TMessage, TKey> :
    IGroupKeyProvider<TMessage, TKey>
    where TMessage : class
    where TKey : struct
{
    readonly Func<ConsumeContext<TMessage>, TKey?> _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public ValueTypeGroupKeyProvider(Func<ConsumeContext<TMessage>, TKey?> provider)
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
        TKey? property = _provider(context);

        if (property.HasValue)
        {
            key = property.Value;
            return true;
        }

        key = default;
        return false;
    }
}
