using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts a reference-type grouping selector to the batch grouping contract.</summary>
/// <typeparam name="TMessage">The message contract supplied to the selector.</typeparam>
/// <typeparam name="TKey">The reference-type grouping key.</typeparam>
public sealed class GroupKeyProvider<TMessage, TKey> :
    IGroupKeyProvider<TMessage, TKey>
    where TMessage : class
    where TKey : class
{
    readonly Func<ConsumeContext<TMessage>, TKey> _provider;

    /// <summary>Creates a grouping-key provider from the supplied selector.</summary>
    /// <param name="provider">The selector evaluated for each message context.</param>
    public GroupKeyProvider(Func<ConsumeContext<TMessage>, TKey> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Evaluates the selector and reports whether it returned a non-null key.</summary>
    /// <param name="context">The message context supplied to the selector.</param>
    /// <param name="key">Receives the selected key when one exists.</param>
    /// <returns><see langword="true" /> for a non-null key; otherwise, <see langword="false" />.</returns>
    public bool TryGetKey(ConsumeContext<TMessage> context, out TKey key)
    {
        ArgumentNullException.ThrowIfNull(context);

        key = _provider(context);

        return key != null;
    }
}
