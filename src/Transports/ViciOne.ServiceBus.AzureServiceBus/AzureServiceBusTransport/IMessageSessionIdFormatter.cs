namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Derives Azure Service Bus session identifiers from typed send contexts.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public interface IMessageSessionIdFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the session identifier for an outgoing message.</summary>
    /// <param name="context">The typed send context.</param>
    /// <returns>The session identifier, or <see langword="null"/> when no identifier should be assigned.</returns>
    string? FormatSessionId(SendContext<TMessage> context);
}
