using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by routing slip executor.</summary>
public interface IRoutingSlipExecutor
{
    /// <summary>Execute a routing slip.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(RoutingSlip routingSlip, CancellationToken cancellationToken = default);
}
