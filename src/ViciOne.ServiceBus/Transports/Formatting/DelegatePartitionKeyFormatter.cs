using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats partition keys by invoking a message-specific delegate.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public sealed class DelegatePartitionKeyFormatter<TMessage> :
    IMessagePartitionKeyFormatter<TMessage>
    where TMessage : class
{
    readonly Func<SendContext<TMessage>, string> _formatter;

    /// <summary>Initializes the formatter with the delegate used for each message.</summary>
    /// <param name="formatter">The delegate that produces a non-null partition key.</param>
    public DelegatePartitionKeyFormatter(Func<SendContext<TMessage>, string> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>Formats the partition key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The partition key produced by the configured delegate.</returns>
    public string FormatPartitionKey(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _formatter(context)
            ?? throw new InvalidOperationException("The partition-key formatter returned null.");
    }
}
