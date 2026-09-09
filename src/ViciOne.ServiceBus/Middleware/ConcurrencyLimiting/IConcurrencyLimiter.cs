using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

/// <summary>Owns a shared concurrency budget that can be adjusted by management commands.</summary>
internal interface IConcurrencyLimiter :
    IConsumer<SetConcurrencyLimit>
{
    /// <summary>Gets the number of permits currently available without waiting.</summary>
    int Available { get; }
    /// <summary>Gets the configured maximum concurrency.</summary>
    int Limit { get; }

    /// <summary>Waits until one concurrency permit is available.</summary>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes after acquiring one permit.</returns>
    Task WaitAsync(CancellationToken cancellationToken);

    /// <summary>Returns one previously acquired permit.</summary>
    void Release();
}
