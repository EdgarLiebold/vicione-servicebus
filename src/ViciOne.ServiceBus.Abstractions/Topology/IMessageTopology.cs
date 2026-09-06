using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the operations required by message topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageTopology<in TMessage>
    where TMessage : class
{
    /// <summary>The entity name formatter for this message type.</summary>
    IMessageEntityNameFormatter<TMessage> EntityNameFormatter { get; }

    /// <summary>The formatted entity name for this message type.</summary>
    string EntityName { get; }
}


/// <summary>Defines the operations required by message topology.</summary>
public interface IMessageTopology :
    IMessageTopologyConfigurationObserverConnector
{
    /// <summary>The entity name formatter used to format message names.</summary>
    IEntityNameFormatter EntityNameFormatter { get; }

    /// <summary>Returns the message topology for the specified message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    IMessageTopology<T> GetMessageTopology<T>()
        where T : class;
}
