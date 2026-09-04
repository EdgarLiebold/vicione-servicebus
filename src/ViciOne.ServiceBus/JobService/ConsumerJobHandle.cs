using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a consumer job handle implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumerJobHandle<T> :
    JobHandle
    where T : class
{
    readonly ConsumeJobContext<T> _context;
    readonly TimeSpan _jobCancellationTimeout;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="task">The task value.</param>
    /// <param name="jobCancellationTimeout">The job cancellation timeout value.</param>
    public ConsumerJobHandle(ConsumeJobContext<T> context, Task task, TimeSpan jobCancellationTimeout)
    {
        _context = context;
        _jobCancellationTimeout = jobCancellationTimeout;
        JobTask = task;
    }

    /// <summary>
    /// Gets the job id value.
    /// </summary>
    public Guid JobId => _context.JobId;
    /// <summary>
    /// Gets the job task value.
    /// </summary>
    public Task JobTask { get; }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CancelAsync(string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_context.CancellationToken.IsCancellationRequested)
            return;

        _context.Cancel(reason);

        try
        {
            await JobTask.OrTimeoutAsync(_jobCancellationTimeout, _context.GetTimeProvider()).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
