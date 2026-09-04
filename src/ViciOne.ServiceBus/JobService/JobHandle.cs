using System;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// A JobHandle contains the JobContext, Task, and provides access to the job control
/// </summary>
public interface JobHandle :
    IAsyncDisposable
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Gets the job task value.
    /// </summary>
    Task JobTask { get; }

    /// <summary>
    /// Cancel the job task
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="reason">The reason used by the operation.</param>
    Task CancelAsync(string? reason, CancellationToken cancellationToken = default);
}
