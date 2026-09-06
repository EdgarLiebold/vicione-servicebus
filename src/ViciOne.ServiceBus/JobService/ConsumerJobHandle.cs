using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Controls the lifetime of consumer job.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConsumerJobHandle<T> :
    JobHandle
    where T : class
{
    readonly ConsumeJobContext<T> _context;
    readonly TimeSpan _jobCancellationTimeout;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="task">The task.</param>
    /// <param name="jobCancellationTimeout">The job cancellation timeout.</param>
    public ConsumerJobHandle(ConsumeJobContext<T> context, Task task, TimeSpan jobCancellationTimeout)
    {
        _context = context;
        _jobCancellationTimeout = jobCancellationTimeout;
        JobTask = task;
    }

    /// <summary>Gets the job id.</summary>
    public Guid JobId => _context.JobId;
    /// <summary>Gets the job task.</summary>
    public Task JobTask { get; }

    /// <summary>Cancels the running job and waits for it to observe cancellation.</summary>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CancelAsync(string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_context.CancellationToken.IsCancellationRequested)
            return;

        _context.Cancel(reason);

        try
        {
            await JobTask.OrTimeoutAsync(_jobCancellationTimeout, _context.GetTimeProvider(), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
