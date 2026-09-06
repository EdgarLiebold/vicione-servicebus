using System.IO;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Converts message data values.</summary>
public static class MessageDataConverter
{
    /// <summary>Exposes the string used by the containing type.</summary>
    public static readonly IMessageDataConverter<string> String = new StringMessageDataConverter();
    /// <summary>Exposes the byte array used by the containing type.</summary>
    public static readonly IMessageDataConverter<byte[]> ByteArray = new ByteArrayMessageDataConverter();
    /// <summary>Exposes the stream used by the containing type.</summary>
    public static readonly IMessageDataConverter<Stream> Stream = new StreamMessageDataConverter();
}
