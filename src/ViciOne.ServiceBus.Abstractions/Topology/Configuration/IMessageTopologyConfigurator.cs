namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageTopologyConfigurator<TMessage> :
    IMessageTypeTopologyConfigurator,
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


/// <summary>Configures message topology.</summary>
public interface IMessageTopologyConfigurator :
    IMessageTopology
{
    /// <summary>Replace the default entity name formatter.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    void SetEntityNameFormatter(IEntityNameFormatter entityNameFormatter);

    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new IMessageTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
