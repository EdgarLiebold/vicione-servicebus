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
    /// <summary>
    /// Performs the format entity name operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public string FormatEntityName<T>()
    {
        return MessageUrn.ForTypeString<T>();
    }
}
