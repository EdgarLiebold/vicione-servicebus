namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats static entity name values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class StaticEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly string _entityName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityName">The entity name.</param>
    public StaticEntityNameFormatter(string entityName)
    {
        _entityName = entityName;
    }

    string IMessageEntityNameFormatter<TMessage>.FormatEntityName()
    {
        return _entityName;
    }
}
