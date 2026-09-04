using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Provides a message name formatter entity name formatter implementation.
/// </summary>
public class MessageNameFormatterEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public MessageNameFormatterEntityNameFormatter(IMessageNameFormatter formatter)
    {
        _formatter = formatter;
    }

    string IEntityNameFormatter.FormatEntityName<T>()
    {
        return _formatter.GetMessageName(typeof(T));
    }
}
