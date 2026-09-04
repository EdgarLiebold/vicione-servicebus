namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageTopologyConfigurator<TMessage> :
    IMessageTypeTopologyConfigurator,
    IMessageTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Sets the entity name formatter used for this message type
    /// </summary>
    /// <param name="entityNameFormatter"></param>
    void SetEntityNameFormatter(IMessageEntityNameFormatter<TMessage> entityNameFormatter);

    /// <summary>
    /// Sets the entity name for this message type
    /// </summary>
    /// <param name="entityName">The entity name</param>
    void SetEntityName(string entityName);
}


/// <summary>
/// Defines the contract for message topology configurator.
/// </summary>
public interface IMessageTopologyConfigurator :
    IMessageTopology
{
    /// <summary>
    /// Replace the default entity name formatter
    /// </summary>
    void SetEntityNameFormatter(IEntityNameFormatter entityNameFormatter);

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IMessageTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
