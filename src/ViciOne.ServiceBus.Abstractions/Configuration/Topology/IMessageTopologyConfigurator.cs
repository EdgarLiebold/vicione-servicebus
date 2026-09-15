namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message topology.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public interface IMessageTopologyConfigurator<TMessage> :
    IMessageTopology<TMessage>
    where TMessage : class
{
    /// <summary>Sets the entity name formatter used for this message type.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    void SetEntityNameFormatter(IMessageEntityNameFormatter<TMessage> entityNameFormatter);

    /// <summary>Sets the entity name for this message type.</summary>
    /// <param name="entityName">The entity name.</param>
    void SetEntityName(string entityName);
}


/// <summary>Configures entity naming for all message contracts.</summary>
public interface IMessageTopologyConfigurator :
    IMessageTopology
{
    /// <summary>Replaces the default entity-name formatter.</summary>
    /// <param name="entityNameFormatter">The new default formatter.</param>
    void SetEntityNameFormatter(IEntityNameFormatter entityNameFormatter);

    /// <summary>Gets the mutable entity-name topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message-specific topology configurator.</returns>
    new IMessageTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
