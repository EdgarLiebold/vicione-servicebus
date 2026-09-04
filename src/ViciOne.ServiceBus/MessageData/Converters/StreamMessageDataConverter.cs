using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Converters;

public class StreamMessageDataConverter :
    IMessageDataConverter<Stream>
{
    public Task<Stream> Convert(Stream stream, CancellationToken cancellationToken)
    {
        return Task.FromResult(stream);
    }
}
