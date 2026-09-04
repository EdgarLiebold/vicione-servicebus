namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Provides a static entity name formatter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class StaticEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly string _entityName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityName">The entity name value.</param>
    public StaticEntityNameFormatter(string entityName)
    {
        _entityName = entityName;
    }

    string IMessageEntityNameFormatter<TMessage>.FormatEntityName()
    {
        return _entityName;
    }
}
