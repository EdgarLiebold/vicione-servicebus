using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts a reference-type grouping selector to the batch grouping contract.</summary>
/// <typeparam name="TMessage">The message contract supplied to the selector.</typeparam>
/// <typeparam name="TKey">The reference-type grouping key.</typeparam>
internal sealed class GroupKeyProvider<TMessage, TKey> :
    IGroupKeyProvider<TMessage, TKey>
    where TMessage : class
    where TKey : class
{
    readonly Func<ConsumeContext<TMessage>, TKey?> _provider;

    /// <summary>Creates a grouping-key provider from the supplied selector.</summary>
    /// <param name="provider">The selector evaluated for each message context.</param>
    public GroupKeyProvider(Func<ConsumeContext<TMessage>, TKey?> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public bool TryGetKey(ConsumeContext<TMessage> context, out TKey key)
    {
        ArgumentNullException.ThrowIfNull(context);

        TKey? selectedKey = _provider(context);
        if (selectedKey is null)
        {
            key = null!;
            return false;
        }

        key = selectedKey;
        return true;
    }
}
