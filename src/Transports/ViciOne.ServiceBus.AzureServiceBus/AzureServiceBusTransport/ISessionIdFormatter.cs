namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Derives Azure Service Bus session identifiers from send contexts.</summary>
public interface ISessionIdFormatter
{
    /// <summary>Formats the session identifier for an outgoing message.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="context">The typed send context.</param>
    /// <returns>The session identifier, or <see langword="null"/> when no identifier should be assigned.</returns>
    string? FormatSessionId<T>(SendContext<T> context)
        where T : class;
}
