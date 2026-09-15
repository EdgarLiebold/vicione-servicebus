namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats broker entity names for message contracts.</summary>
public interface IEntityNameFormatter
{
    /// <summary>Formats the entity name for the given message type.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The non-empty broker entity name.</returns>
    string FormatEntityName<T>();
}
