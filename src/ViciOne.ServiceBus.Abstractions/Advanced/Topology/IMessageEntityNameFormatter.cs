namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats the broker entity name for one message contract.</summary>
/// <typeparam name="TMessage">The message contract type.</typeparam>
public interface IMessageEntityNameFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the broker entity name for the message contract.</summary>
    /// <returns>The non-empty broker entity name.</returns>
    string FormatEntityName();
}
