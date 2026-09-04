using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Provides a message entity name formatter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageEntityNameFormatter<TMessage> :
    IMessageEntityNameFormatter<TMessage>
    where TMessage : class
{
    readonly IEntityNameFormatter _entityNameFormatter;
    string? _entityName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityNameFormatter">The entity name formatter value.</param>
    public MessageEntityNameFormatter(IEntityNameFormatter entityNameFormatter)
    {
        _entityNameFormatter = entityNameFormatter;

        InitializeEntityNameFromAttributeIfSpecified();
    }

    /// <summary>
    /// Not sure it ever makes sense to pass the actual message, but many, someday.
    /// </summary>
    /// <returns></returns>
    public string FormatEntityName()
    {
        return _entityName ??= _entityNameFormatter.FormatEntityName<TMessage>();
    }

    void InitializeEntityNameFromAttributeIfSpecified()
    {
        var entityNameAttribute = typeof(TMessage).GetCustomAttribute<EntityNameAttribute>();
        if (entityNameAttribute != null)
            _entityName = entityNameAttribute.EntityName;
        else if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(Fault<>), out Type[] messageTypes))
        {
            var faultEntityNameAttribute = messageTypes[0].GetCustomAttribute<FaultEntityNameAttribute>();
            if (faultEntityNameAttribute != null)
                _entityName = faultEntityNameAttribute.EntityName;
        }
    }
}
