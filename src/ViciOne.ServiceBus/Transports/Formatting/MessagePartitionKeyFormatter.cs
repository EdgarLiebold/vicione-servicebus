using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts a non-generic partition-key formatter to a message-specific contract.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public sealed class MessagePartitionKeyFormatter<TMessage> :
    IMessagePartitionKeyFormatter<TMessage>
    where TMessage : class
{
    readonly IPartitionKeyFormatter _formatter;

    /// <summary>Initializes the adapter with its underlying formatter.</summary>
    /// <param name="formatter">The formatter that handles arbitrary message contracts.</param>
    public MessagePartitionKeyFormatter(IPartitionKeyFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>Formats the partition key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The partition key produced by the underlying formatter.</returns>
    public string FormatPartitionKey(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _formatter.FormatPartitionKey(context)
            ?? throw new InvalidOperationException("The partition-key formatter returned null.");
    }
}
