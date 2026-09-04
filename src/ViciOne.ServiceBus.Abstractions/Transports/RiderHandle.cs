using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface RiderHandle
{
    Task Ready { get; }
    Task StopAsync(CancellationToken cancellationToken);
}
