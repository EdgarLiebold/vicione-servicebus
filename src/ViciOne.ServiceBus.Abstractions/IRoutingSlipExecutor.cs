using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus;

public interface IRoutingSlipExecutor
{
    /// <summary>
    /// Execute a routing slip
    /// </summary>
    /// <param name="routingSlip"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ExecuteAsync(RoutingSlip routingSlip, CancellationToken cancellationToken = default);
}
