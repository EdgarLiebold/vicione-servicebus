using System.IO;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>
/// Provides a message data reader factory implementation.
/// </summary>
public static class MessageDataReaderFactory
{
    /// <summary>
    /// Creates reader.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IMessageDataReader<T> CreateReader<T>()
    {
        if (typeof(T) == typeof(string))
            return new StringMessageDataReader<T>();

        if (typeof(T) == typeof(byte[]))
            return new BytesMessageDataReader<T>();

        if (typeof(T) == typeof(Stream))
            return new StreamMessageDataReader<T>();

        if (TypeMetadataCache.IsValidMessageDataType(typeof(T)))
            return new ObjectMessageDataReader<T>();

        throw new MessageDataException("Unsupported message data type: " + TypeCache<T>.ShortName);
    }
}
