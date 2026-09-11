namespace ViciOne.ServiceBus.MessageData.Serialization;

/// <summary>Writes an inline message-data value into its transport reference envelope.</summary>
internal interface IInlineMessageData
{
    /// <summary>Copies the inline representation into the supplied reference envelope.</summary>
    /// <param name="reference">The target reference envelope.</param>
    void Set(IMessageDataReference reference);
}
