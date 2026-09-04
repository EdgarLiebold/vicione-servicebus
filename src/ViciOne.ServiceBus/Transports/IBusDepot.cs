using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

internal interface IBusDepot
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
