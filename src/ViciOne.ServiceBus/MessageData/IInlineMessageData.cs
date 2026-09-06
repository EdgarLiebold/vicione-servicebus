namespace ViciOne.ServiceBus.MessageData;

/// <summary>Defines the operations required by inline message data.</summary>
public interface IInlineMessageData
{
    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="reference">The reference.</param>
    void Set(IMessageDataReference reference);
}
