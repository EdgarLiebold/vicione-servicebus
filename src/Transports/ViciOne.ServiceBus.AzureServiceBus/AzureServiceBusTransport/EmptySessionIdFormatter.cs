namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Formats empty session id values.</summary>
public class EmptySessionIdFormatter :
    ISessionIdFormatter
{
    string? ISessionIdFormatter.FormatSessionId<T>(SendContext<T> context)
    {
        return null;
    }
}
