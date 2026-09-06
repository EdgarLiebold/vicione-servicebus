namespace ViciOne.ServiceBus.MessageData;

/// <summary>Defines the operations required by message data reference.</summary>
public interface IMessageDataReference
{
    /// <summary>Gets or sets the text.</summary>
    string? Text { set; }
    /// <summary>Gets or sets the data.</summary>
    byte[]? Data { set; }
}
