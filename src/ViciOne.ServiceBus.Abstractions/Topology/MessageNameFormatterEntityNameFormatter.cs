using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats message name formatter entity name values.</summary>
public class MessageNameFormatterEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="formatter">The formatter.</param>
    public MessageNameFormatterEntityNameFormatter(IMessageNameFormatter formatter)
    {
        _formatter = formatter;
    }

    string IEntityNameFormatter.FormatEntityName<T>()
    {
        return _formatter.GetMessageName(typeof(T));
    }
}
