using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Extensibility;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Creates scheduled-message jobs for dependency-injected or standalone Quartz schedulers.</summary>
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

    /// <summary>Creates a factory that supplies explicit dependencies to every scheduled-message job.</summary>
    /// <param name="bus">The bus used to resolve destination endpoints.</param>
    /// <param name="timeProvider">The clock used to derive remaining message time to live.</param>
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

    /// <summary>Creates the job scope used to execute a scheduled message.</summary>
    /// <param name="bundle">The fired-trigger bundle; job construction does not inspect it.</param>
    /// <param name="scheduler">The active scheduler; standalone jobs later resolve dependencies from its context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed value task containing the scheduled-message job scope.</returns>
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

    /// <summary>Releases a job scope after execution.</summary>
    /// <param name="scope">The completed Quartz job scope.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask ReturnJobAsync(JobScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (scope.Job is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else if (scope.Job is IDisposable disposable)
            disposable.Dispose();
    }
}
