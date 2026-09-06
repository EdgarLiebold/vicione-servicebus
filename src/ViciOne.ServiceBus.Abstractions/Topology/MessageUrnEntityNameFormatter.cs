namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// This is the simplest thing, it uses the built-in URN for a message type
/// as the entity name, which can include illegal characters for most message
/// brokers. It's nice for in-memory though, which doesn't give a hoot about the
/// string.
/// </summary>
public class MessageUrnEntityNameFormatter :
    IEntityNameFormatter
{
    /// <summary>Formats entity name.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The formatted entity name.</returns>
    public string FormatEntityName<T>()
    {
        return MessageUrn.ForTypeString<T>();
    }
}
