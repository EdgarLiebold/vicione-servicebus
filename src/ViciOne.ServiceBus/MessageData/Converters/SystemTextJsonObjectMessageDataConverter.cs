using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Converts system text json object message data values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SystemTextJsonObjectMessageDataConverter<T> :
    IMessageDataConverter<T>
{
    readonly JsonSerializerOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public SystemTextJsonObjectMessageDataConverter(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public async Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
