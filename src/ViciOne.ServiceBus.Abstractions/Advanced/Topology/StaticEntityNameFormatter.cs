namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Returns one explicitly configured entity name for a message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public sealed class StaticEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly string _entityName;

    /// <summary>Creates a formatter for the specified entity name.</summary>
    /// <param name="entityName">The non-empty entity name returned by this formatter.</param>
    public StaticEntityNameFormatter(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        _entityName = entityName;
    }

    string IMessageEntityNameFormatter<TMessage>.FormatEntityName()
    {
        return _entityName;
    }
}
