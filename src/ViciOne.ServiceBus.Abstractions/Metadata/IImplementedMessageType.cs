namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for implemented message type.
/// </summary>
public interface IImplementedMessageType
{
    /// <summary>
    /// Performs the implements message type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="direct">The direct value.</param>
    void ImplementsMessageType<T>(bool direct)
        where T : class;
}
