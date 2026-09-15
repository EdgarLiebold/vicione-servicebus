using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Exposes broker entity naming for one message contract.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public interface IMessageTopology<in TMessage>
    where TMessage : class
{
    /// <summary>Gets the entity-name formatter for this message contract.</summary>
    IMessageEntityNameFormatter<TMessage> EntityNameFormatter { get; }

    /// <summary>Gets the formatted broker entity name.</summary>
    string EntityName { get; }
}


/// <summary>Provides broker entity naming for all message contracts.</summary>
public interface IMessageTopology :
    IMessageTopologyConfigurationObserverConnector
{
    /// <summary>Gets the default formatter used by message topologies.</summary>
    IEntityNameFormatter EntityNameFormatter { get; }

    /// <summary>Gets topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The entity-name topology for <typeparamref name="T" />.</returns>
    IMessageTopology<T> GetMessageTopology<T>()
        where T : class;
}
