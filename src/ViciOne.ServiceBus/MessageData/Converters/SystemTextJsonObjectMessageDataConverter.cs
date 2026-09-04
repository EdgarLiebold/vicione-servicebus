using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>
/// Provides a system text json object message data converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SystemTextJsonObjectMessageDataConverter<T> :
    IMessageDataConverter<T>
{
    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public SystemTextJsonObjectMessageDataConverter(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
