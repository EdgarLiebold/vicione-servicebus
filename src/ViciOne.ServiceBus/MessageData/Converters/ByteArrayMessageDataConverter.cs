// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.Converters
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Metadata;


    public class ByteArrayMessageDataConverter :
        IMessageDataConverter<byte[]>
    {
        public async Task<byte[]> Convert(Stream stream, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();

            await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

            return ms.ToArray();
        }
    }
}
