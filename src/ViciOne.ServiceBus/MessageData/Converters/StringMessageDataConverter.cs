using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Decodes repository content as UTF-8 text.</summary>
internal sealed class StringMessageDataConverter :
    IMessageDataConverter<string>
{
    /// <inheritdoc />
    public bool TransfersSourceStreamOwnership => false;

    /// <summary>Reads the remaining stream content as UTF-8 text.</summary>
    /// <param name="stream">The readable UTF-8 source stream.</param>
    /// <param name="cancellationToken">The token that cancels stream reading.</param>
    /// <returns>A task containing the decoded text.</returns>
    public async Task<string?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
