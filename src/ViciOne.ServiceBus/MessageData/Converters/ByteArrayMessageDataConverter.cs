using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>
/// Provides a byte array message data converter implementation.
/// </summary>
public class ByteArrayMessageDataConverter :
    IMessageDataConverter<byte[]>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<byte[]?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return ms.ToArray();
    }
}
