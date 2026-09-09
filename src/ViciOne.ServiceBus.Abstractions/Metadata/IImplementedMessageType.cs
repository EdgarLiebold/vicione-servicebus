namespace ViciOne.ServiceBus.Metadata;

/// <summary>Receives a strongly typed message-topology parent discovered at run time.</summary>
public interface IImplementedMessageType
{
    /// <summary>Processes one implemented message contract.</summary>
    /// <typeparam name="T">The implemented message contract.</typeparam>
    /// <param name="direct">Whether the contract is an immediate topology parent.</param>
    void ImplementsMessageType<T>(bool direct)
        where T : class;
}
