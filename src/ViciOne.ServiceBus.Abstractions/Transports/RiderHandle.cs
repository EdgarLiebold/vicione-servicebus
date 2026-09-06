using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Controls the lifetime of rider.</summary>
public interface RiderHandle
{
    /// <summary>Gets the ready.</summary>
    Task Ready { get; }
    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
