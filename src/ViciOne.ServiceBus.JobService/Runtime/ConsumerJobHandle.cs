using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Owns the execution task, cancellation, and resources of one local job attempt.</summary>
/// <typeparam name="TJob">The job contract type.</typeparam>
internal sealed class ConsumerJobHandle<TJob> :
    JobHandle
    where TJob : class
{
    readonly TaskCompletionSource _abandoned;
    readonly ConsumeJobContext<TJob> _context;
    readonly TimeSpan _jobCancellationTimeout;

    /// <summary>Creates a handle over an admitted execution attempt.</summary>
    /// <param name="context">The execution context whose cancellation and resources the handle owns.</param>
    /// <param name="completion">The consumer-pipeline completion task.</param>
    /// <param name="jobCancellationTimeout">The maximum time to wait after requesting cancellation.</param>
    public ConsumerJobHandle(ConsumeJobContext<TJob> context, Task completion, TimeSpan jobCancellationTimeout)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(completion);
        if (jobCancellationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(jobCancellationTimeout), jobCancellationTimeout, "The cancellation timeout must be greater than zero.");

        _jobCancellationTimeout = jobCancellationTimeout;
        Execution = completion;
        _abandoned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Completion = ObserveExecutionAsync(completion, _abandoned.Task);
    }

    /// <inheritdoc />
    public Guid JobId => _context.JobId;
    /// <inheritdoc />
    public Guid AttemptId => _context.AttemptId;
    /// <inheritdoc />
    public Task Execution { get; }
    /// <inheritdoc />
    public Task Completion { get; }

    /// <summary>Cancels the running job and waits for it to observe cancellation.</summary>
    /// <param name="reason">The optional reason published with the cancellation outcome.</param>
    /// <param name="cancellationToken">The token that cancels waiting for the consumer to stop.</param>
    /// <returns>A task that completes when the consumer stops or its grace period expires.</returns>
    public async Task CancelAsync(string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_context.CancellationToken.IsCancellationRequested)
            _context.Cancel(reason);

        try
        {
            await Execution.OrTimeoutAsync(_jobCancellationTimeout, _context.GetTimeProvider(), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _abandoned.TrySetResult();
        }
    }

    /// <summary>Releases the execution context and its cancellation resources.</summary>
    /// <returns>A task that completes when the context is disposed.</returns>
    public ValueTask DisposeAsync()
    {
        _abandoned.TrySetResult();
        return _context.DisposeAsync();
    }

    static async Task ObserveExecutionAsync(Task execution, Task abandoned)
    {
        Task completed = await Task.WhenAny(execution, abandoned).ConfigureAwait(false);
        if (completed == execution)
            await execution.ConfigureAwait(false);
        else
            execution.IgnoreUnobservedExceptions();
    }
}
