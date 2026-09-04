using System.IO;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>
/// Provides a message data converter implementation.
/// </summary>
public static class MessageDataConverter
{
    /// <summary>
    /// Defines the string value.
    /// </summary>
    public static readonly IMessageDataConverter<string> String = new StringMessageDataConverter();
    /// <summary>
    /// Defines the byte array value.
    /// </summary>
    public static readonly IMessageDataConverter<byte[]> ByteArray = new ByteArrayMessageDataConverter();
    /// <summary>
    /// Defines the stream value.
    /// </summary>
    public static readonly IMessageDataConverter<Stream> Stream = new StreamMessageDataConverter();
}
