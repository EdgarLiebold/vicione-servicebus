using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>
/// Provides a string message data converter implementation.
/// </summary>
public class StringMessageDataConverter :
    IMessageDataConverter<string>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<string?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
