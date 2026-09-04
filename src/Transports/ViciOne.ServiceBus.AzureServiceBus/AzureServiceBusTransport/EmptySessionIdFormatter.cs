namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides an empty session id formatter implementation.
/// </summary>
public class EmptySessionIdFormatter :
    ISessionIdFormatter
{
    string? ISessionIdFormatter.FormatSessionId<T>(SendContext<T> context)
    {
        return null;
    }
}
