using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Metadata;

public interface IMessageDataConverter<T>
{
    Task<T> Convert(Stream stream, CancellationToken cancellationToken);
}
