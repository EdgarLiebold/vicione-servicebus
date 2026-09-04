namespace ViciOne.ServiceBus.MessageData;

/// <summary>
/// Defines the contract for message data reference.
/// </summary>
public interface IMessageDataReference
{
    /// <summary>
    /// Gets or sets the text value.
    /// </summary>
    string? Text { set; }
    /// <summary>
    /// Gets or sets the data value.
    /// </summary>
    byte[]? Data { set; }
}
