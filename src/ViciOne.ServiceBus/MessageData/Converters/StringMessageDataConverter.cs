using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Converts string message data values.</summary>
public class StringMessageDataConverter :
    IMessageDataConverter<string>
{
    /// <summary>Converts the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public async Task<string?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
