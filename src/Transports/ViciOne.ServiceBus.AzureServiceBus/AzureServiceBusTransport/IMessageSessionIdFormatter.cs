namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for message session id formatter.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageSessionIdFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the format session id operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string? FormatSessionId(SendContext<TMessage> context);
}
