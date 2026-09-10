using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Deserializes a message-data payload from its repository stream.</summary>
/// <typeparam name="TValue">The message-data value type.</typeparam>
public interface IMessageDataConverter<TValue>
{
    /// <summary>Reads one message-data value from a stream.</summary>
    /// <param name="stream">The readable payload stream.</param>
    /// <param name="cancellationToken">Cancels stream reading and deserialization.</param>
    /// <returns>The deserialized value, or <see langword="null" /> when the payload represents no value.</returns>
    Task<TValue?> ConvertAsync(Stream stream, CancellationToken cancellationToken);
}
