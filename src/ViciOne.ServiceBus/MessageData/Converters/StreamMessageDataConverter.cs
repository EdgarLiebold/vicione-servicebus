using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>
/// Provides a stream message data converter implementation.
/// </summary>
public class StreamMessageDataConverter :
    IMessageDataConverter<Stream>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Stream?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.IO.Stream?>(cancellationToken); return Task.FromResult<Stream?>(stream);
    }
}
