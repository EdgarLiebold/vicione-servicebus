using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IBusDepot
{
    Task Start(CancellationToken cancellationToken);

    Task Stop(CancellationToken cancellationToken);
}
