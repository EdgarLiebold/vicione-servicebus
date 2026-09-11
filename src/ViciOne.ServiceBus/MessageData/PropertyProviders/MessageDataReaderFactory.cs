using System.IO;
using ViciOne.ServiceBus.MessageData.Internals;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Selects the repository reader for a supported message-data value type.</summary>
internal static class MessageDataReaderFactory
{
    /// <summary>Creates a reader for a built-in scalar, stream, binary, or object contract.</summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <returns>The reader for the requested value category.</returns>
    public static IMessageDataReader<T> CreateReader<T>()
    {
        if (typeof(T) == typeof(string))
            return new StringMessageDataReader<T>();

        if (typeof(T) == typeof(byte[]))
            return new BytesMessageDataReader<T>();

        if (typeof(T) == typeof(Stream))
            return new StreamMessageDataReader<T>();

        if (MessageDataTypeClassifier.IsSupported(typeof(T)))
            return new ObjectMessageDataReader<T>();

        throw new MessageDataException("Unsupported message data type: " + TypeCache<T>.ShortName);
    }
}
