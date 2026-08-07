// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.Converters
{
    using System.IO;
    using Metadata;


    public static class MessageDataConverter
    {
        public static readonly IMessageDataConverter<string> String = new StringMessageDataConverter();
        public static readonly IMessageDataConverter<byte[]> ByteArray = new ByteArrayMessageDataConverter();
        public static readonly IMessageDataConverter<Stream> Stream = new StreamMessageDataConverter();
    }
}
