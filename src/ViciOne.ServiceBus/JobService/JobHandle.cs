using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>A JobHandle contains the JobContext, Task, and provides access to the job control.</summary>
public interface JobHandle :
    IAsyncDisposable
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }

    /// <summary>Gets the job task.</summary>
    Task JobTask { get; }

    /// <summary>Cancel the job task.</summary>
    /// <param name="reason">The reason used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelAsync(string? reason, CancellationToken cancellationToken = default);
}
