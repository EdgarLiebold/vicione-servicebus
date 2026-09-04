using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Metadata;

public interface IMessageDataConverter<T>
{
    Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken);
}
