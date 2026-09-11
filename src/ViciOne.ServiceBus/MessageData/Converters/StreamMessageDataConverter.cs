using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Transfers a repository stream directly to the message-data caller.</summary>
internal sealed class StreamMessageDataConverter :
    IMessageDataConverter<Stream>
{
    /// <inheritdoc />
    public bool TransfersSourceStreamOwnership => true;

    /// <summary>Returns the repository stream itself and transfers its ownership to the caller.</summary>
    /// <param name="stream">The repository stream to return.</param>
    /// <param name="cancellationToken">The token that cancels the transfer.</param>
    /// <returns>A task containing the same stream instance.</returns>
    public Task<Stream?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<Stream?>(cancellationToken)
            : Task.FromResult<Stream?>(stream);
    }
}
