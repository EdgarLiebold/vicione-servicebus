using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes readiness and lifetime control for a started transport host.</summary>
public interface IHostHandle
{
    /// <summary>Gets a task that completes when every endpoint and rider owned by the host is ready.</summary>
    Task<HostReady> Ready { get; }

    /// <summary>Stops the host and releases its transport resources.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the host has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
