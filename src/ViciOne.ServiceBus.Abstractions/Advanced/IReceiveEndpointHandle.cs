using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes readiness and lifetime control for a started receive endpoint.</summary>
public interface IReceiveEndpointHandle
{
    /// <summary>
    /// Gets a task that completes with the endpoint's readiness state, faults when startup fails, or is canceled when the
    /// endpoint stops before becoming ready.
    /// </summary>
    Task<ReceiveEndpointReady> Ready { get; }

    /// <summary>Stops the endpoint and releases its transport resources.</summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that completes after the endpoint has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
