// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.Converters
{
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Metadata;


    public class SystemTextJsonObjectMessageDataConverter<T> :
        IMessageDataConverter<T>
    {
        readonly JsonSerializerOptions _options;

        public SystemTextJsonObjectMessageDataConverter(JsonSerializerOptions options)
        {
            _options = options;
        }

        public async Task<T> Convert(Stream stream, CancellationToken cancellationToken)
        {
            var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
