using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Adapts transport message-name formatting to broker entity-name formatting.</summary>
public sealed class MessageNameFormatterEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Creates an adapter for the specified transport formatter.</summary>
    /// <param name="formatter">The transport message-name formatter.</param>
    public MessageNameFormatterEntityNameFormatter(IMessageNameFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    string IEntityNameFormatter.FormatEntityName<T>()
    {
        string entityName = _formatter.GetMessageName(typeof(T));
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName, "formatter");
        return entityName;
    }
}
