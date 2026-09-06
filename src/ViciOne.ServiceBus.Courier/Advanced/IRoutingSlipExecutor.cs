using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Executes routing slips through their configured activity itinerary.</summary>
public interface IRoutingSlipExecutor
{
    /// <summary>Starts execution of a routing slip.</summary>
    /// <param name="routingSlip">The routing slip itinerary and variables.</param>
    /// <param name="cancellationToken">The token that cancels submission.</param>
    /// <returns>A task that completes when the routing slip has been submitted for execution.</returns>
    Task ExecuteAsync(RoutingSlip routingSlip, CancellationToken cancellationToken = default);
}
