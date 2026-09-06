namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Used to build entity names for the publish topology.</summary>
public interface IEntityNameFormatter
{
    /// <summary>Formats the entity name for the given message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The formatted entity name.</returns>
    string FormatEntityName<T>();
}
