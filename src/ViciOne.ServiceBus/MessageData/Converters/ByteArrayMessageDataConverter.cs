using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Reads repository content into an independent byte-array snapshot.</summary>
internal sealed class ByteArrayMessageDataConverter :
    IMessageDataConverter<byte[]>
{
    /// <inheritdoc />
    public bool TransfersSourceStreamOwnership => false;

    /// <summary>Reads the remaining stream content into a new byte array.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="cancellationToken">The token that cancels stream reading.</param>
    /// <returns>A task containing an independent byte-array snapshot.</returns>
    public async Task<byte[]?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return ms.ToArray();
    }
}
