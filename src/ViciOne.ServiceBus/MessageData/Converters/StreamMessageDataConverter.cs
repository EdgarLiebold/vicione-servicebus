using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

public class StreamMessageDataConverter :
    IMessageDataConverter<Stream>
{
    public Task<Stream?> ConvertAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.IO.Stream?>(cancellationToken); return Task.FromResult<Stream?>(stream);
    }
}
