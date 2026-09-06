namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Used to build entity names for the publish topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageEntityNameFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the entity name for the given message.</summary>
    /// <returns>The formatted entity name.</returns>
    string FormatEntityName();
}
