using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a delegate session id formatter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DelegateSessionIdFormatter<TMessage> :
    IMessageSessionIdFormatter<TMessage>
    where TMessage : class
{
    readonly Func<SendContext<TMessage>, string> _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public DelegateSessionIdFormatter(Func<SendContext<TMessage>, string> formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Performs the format session id operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public string FormatSessionId(SendContext<TMessage> context)
    {
        return _formatter(context) ?? "";
    }
}
