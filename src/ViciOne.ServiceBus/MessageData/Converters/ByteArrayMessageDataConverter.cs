using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Converts byte array message data values.</summary>
public class ByteArrayMessageDataConverter :
    IMessageDataConverter<byte[]>
{
    /// <summary>Converts the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public async Task<byte[]?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return ms.ToArray();
    }
}
