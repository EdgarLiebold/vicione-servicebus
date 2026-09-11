namespace ViciOne.ServiceBus.MessageData.Serialization;

/// <summary>Accepts the mutually exclusive text or binary representation of inline message data.</summary>
internal interface IMessageDataReference
{
    /// <summary>Sets inline UTF-8 text content.</summary>
    string? Text { set; }

    /// <summary>Sets inline binary content.</summary>
    byte[]? Data { set; }
}
