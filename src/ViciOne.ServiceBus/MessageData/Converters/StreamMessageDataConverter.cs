using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Converts stream message data values.</summary>
public class StreamMessageDataConverter :
    IMessageDataConverter<Stream>
{
    /// <summary>Converts the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<Stream?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.IO.Stream?>(cancellationToken); return Task.FromResult<Stream?>(stream);
    }
}
