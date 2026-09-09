namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects an optional grouping key for a message entering a batch collector.</summary>
/// <typeparam name="TMessage">The message contract supplied to the selector.</typeparam>
/// <typeparam name="TKey">The grouping-key type.</typeparam>
public interface IGroupKeyProvider<in TMessage, TKey>
    where TMessage : class
{
    /// <summary>Attempts to select a non-null grouping key.</summary>
    /// <param name="context">The message context supplied to the selector.</param>
    /// <param name="key">Receives the selected key when one exists.</param>
    /// <returns><see langword="true" /> when the message has a grouping key; otherwise, <see langword="false" />.</returns>
    bool TryGetKey(ConsumeContext<TMessage> context, out TKey key);
}
