using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a delegate routing key formatter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DelegateRoutingKeyFormatter<TMessage> :
    IMessageRoutingKeyFormatter<TMessage>
    where TMessage : class
{
    readonly Func<SendContext<TMessage>, string> _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public DelegateRoutingKeyFormatter(Func<SendContext<TMessage>, string> formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Performs the format routing key operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public string FormatRoutingKey(SendContext<TMessage> context)
    {
        return _formatter(context);
    }
}
