using System.IO;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Provides stateless converters for the built-in message-data value categories.</summary>
internal static class MessageDataConverter
{
    /// <summary>Gets the UTF-8 text converter.</summary>
    internal static IMessageDataConverter<string> String { get; } = new StringMessageDataConverter();

    /// <summary>Gets the binary snapshot converter.</summary>
    internal static IMessageDataConverter<byte[]> ByteArray { get; } = new ByteArrayMessageDataConverter();

    /// <summary>Gets the ownership-transferring stream converter.</summary>
    internal static IMessageDataConverter<Stream> Stream { get; } = new StreamMessageDataConverter();
}
