namespace ViciOne.ServiceBus.MessageData;

/// <summary>
/// Defines the contract for inline message data.
/// </summary>
public interface IInlineMessageData
{
    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="reference">The reference value.</param>
    void Set(IMessageDataReference reference);
}
