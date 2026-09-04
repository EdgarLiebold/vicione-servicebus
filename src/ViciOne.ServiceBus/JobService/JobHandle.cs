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
    Guid JobId { get; }

    Task JobTask { get; }

    /// <summary>
    /// Cancel the job task
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="reason">The reason used by the operation.</param>
    Task CancelAsync(string? reason, CancellationToken cancellationToken = default);
}
