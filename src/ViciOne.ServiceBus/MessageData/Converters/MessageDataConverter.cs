using System.IO;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

public static class MessageDataConverter
{
    public static readonly IMessageDataConverter<string> String = new StringMessageDataConverter();
    public static readonly IMessageDataConverter<byte[]> ByteArray = new ByteArrayMessageDataConverter();
    public static readonly IMessageDataConverter<Stream> Stream = new StreamMessageDataConverter();
}
