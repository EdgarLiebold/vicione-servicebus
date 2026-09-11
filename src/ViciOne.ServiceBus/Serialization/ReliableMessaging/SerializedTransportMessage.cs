namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Identifies a dispatch whose serializer supplies an already encoded transport body instead of serializing the
/// marker itself.
/// </summary>
public sealed class SerializedTransportMessage
{
    SerializedTransportMessage()
    {
    }

    /// <summary>Gets the shared marker used for pre-serialized transport dispatch.</summary>
    public static SerializedTransportMessage Instance { get; } = new();
}
