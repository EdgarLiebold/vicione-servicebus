using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Converters;

/// <summary>Deserializes object-valued message data with System.Text.Json.</summary>
/// <typeparam name="T">The object contract type.</typeparam>
internal sealed class SystemTextJsonObjectMessageDataConverter<T> :
    IMessageDataConverter<T>
{
    readonly JsonSerializerOptions _options;

    /// <summary>Creates a converter with the serializer options used by the owning transport context.</summary>
    /// <param name="options">The serializer options used for deserialization.</param>
    public SystemTextJsonObjectMessageDataConverter(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool TransfersSourceStreamOwnership => false;

    /// <summary>Deserializes one value from the remaining UTF-8 JSON content.</summary>
    /// <param name="stream">The readable JSON source stream.</param>
    /// <param name="cancellationToken">The token that cancels deserialization.</param>
    /// <returns>A task containing the deserialized value.</returns>
    public async Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
