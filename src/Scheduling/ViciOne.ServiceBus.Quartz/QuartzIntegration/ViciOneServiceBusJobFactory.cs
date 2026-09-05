using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Extensibility;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Provides a vici one service bus job factory implementation.
/// </summary>
public class ViciOneServiceBusJobFactory :
    IJobFactory
{
    readonly IBus? _bus;
    readonly TimeProvider? _timeProvider;

    /// <summary>
    /// Creates a factory that resolves the bus and clock from the Quartz scheduler context populated by
    /// <c>ConfigureInMemoryScheduler</c>.
    /// </summary>
    public ViciOneServiceBusJobFactory()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ViciOneServiceBusJobFactory(IBus bus, TimeProvider timeProvider)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    ValueTask<JobScope> IJobFactory.CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken)
    {
        return CreateJobAsync(bundle, scheduler, cancellationToken);
    }

    ValueTask IJobFactory.ReturnJob(JobScope scope, CancellationToken cancellationToken)
    {
        return ReturnJobAsync(scope, cancellationToken);
    }

    /// <summary>
    /// Creates the job scope used to execute a scheduled message.
    /// </summary>
    /// <param name="bundle">The bundle used by the operation.</param>
    /// <param name="scheduler">The scheduler used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ValueTask<JobScope> CreateJobAsync(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(scheduler);
        cancellationToken.ThrowIfCancellationRequested();

        IJob job = _bus is not null && _timeProvider is not null
            ? new ScheduledMessageJob(_bus, _timeProvider)
            : new ScheduledMessageJob();

        return ValueTask.FromResult(new JobScope(job, state: null));
    }

    /// <summary>
    /// Releases a job scope after execution.
    /// </summary>
    /// <param name="scope">The scope used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async ValueTask ReturnJobAsync(JobScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (scope.Job is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else if (scope.Job is IDisposable disposable)
            disposable.Dispose();
    }
}
