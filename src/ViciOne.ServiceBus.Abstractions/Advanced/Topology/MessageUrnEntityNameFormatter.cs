namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Uses the canonical message URN as an entity name for transports that accept URN characters.</summary>
public sealed class MessageUrnEntityNameFormatter :
    IEntityNameFormatter
{
    /// <summary>Returns the canonical message URN.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message contract's canonical URN.</returns>
    public string FormatEntityName<T>()
    {
        return MessageUrn.ForTypeString<T>();
    }
}
