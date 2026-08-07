// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Metadata
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;


    public interface IMessageDataConverter<T>
    {
        Task<T> Convert(Stream stream, CancellationToken cancellationToken);
    }
}
