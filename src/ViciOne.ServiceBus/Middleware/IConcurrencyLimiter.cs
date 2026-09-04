using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for concurrency limiter.
/// </summary>
public interface IConcurrencyLimiter :
    IConsumer<SetConcurrencyLimit>
{
    /// <summary>
    /// Gets the available value.
    /// </summary>
    int Available { get; }
    /// <summary>
    /// Gets the limit value.
    /// </summary>
    int Limit { get; }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task WaitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    void Release();
}
