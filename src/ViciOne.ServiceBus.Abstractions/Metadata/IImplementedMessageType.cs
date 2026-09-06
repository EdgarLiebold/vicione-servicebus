namespace ViciOne.ServiceBus.Metadata;

/// <summary>Defines the operations required by implemented message type.</summary>
public interface IImplementedMessageType
{
    /// <summary>Determines whether the type implements the message contract.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="direct">The direct.</param>
    void ImplementsMessageType<T>(bool direct)
        where T : class;
}
