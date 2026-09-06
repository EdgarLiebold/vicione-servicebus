using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Defines the operations required by concurrency limiter.</summary>
public interface IConcurrencyLimiter :
    IConsumer<SetConcurrencyLimit>
{
    /// <summary>Gets the available.</summary>
    int Available { get; }
    /// <summary>Gets the limit.</summary>
    int Limit { get; }

    /// <summary>Waits for the configured condition.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WaitAsync(CancellationToken cancellationToken);

    /// <summary>Releases the owned resource.</summary>
    void Release();
}
