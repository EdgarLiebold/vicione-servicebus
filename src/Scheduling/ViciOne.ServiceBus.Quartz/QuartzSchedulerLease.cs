using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Represents the resources created for a directly configured Quartz scheduling endpoint.</summary>
public sealed class QuartzSchedulerLease : IAsyncDisposable
{
    readonly IAsyncDisposable _partitioner;
    readonly bool _ownsSchedulerFactory;
    readonly bool _waitForJobsToComplete;
    ISchedulerFactory? _schedulerFactory;

    internal QuartzSchedulerLease(
        Uri endpointAddress,
        ISchedulerFactory schedulerFactory,
        bool ownsSchedulerFactory,
        bool waitForJobsToComplete,
        IAsyncDisposable partitioner)
    {
        EndpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
        _ownsSchedulerFactory = ownsSchedulerFactory;
        _waitForJobsToComplete = waitForJobsToComplete;
    }

    /// <summary>Gets the scheduling endpoint configured on the bus.</summary>
    public Uri EndpointAddress { get; }

    /// <summary>Gets the configured scheduler factory until the lease is disposed.</summary>
    public ISchedulerFactory SchedulerFactory => _schedulerFactory
        ?? throw new ObjectDisposedException(nameof(QuartzSchedulerLease));

    /// <summary>Releases the endpoint resources and, when applicable, the adapter-owned scheduler factory.</summary>
    /// <returns>An operation that completes after every owned resource has been released.</returns>
    public async ValueTask DisposeAsync()
    {
        ISchedulerFactory? factory = Interlocked.Exchange(ref _schedulerFactory, null);
        if (factory is null)
            return;

        List<Exception>? failures = null;
        try
        {
            await _partitioner.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            AddFailure(ref failures, exception);
        }

        if (_ownsSchedulerFactory)
        {
            try
            {
                foreach (IScheduler scheduler in await factory.GetAllSchedulers().ConfigureAwait(false))
                {
                    if (scheduler.Status != SchedulerStatus.Shutdown)
                        await scheduler.Shutdown(_waitForJobsToComplete).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                AddFailure(ref failures, exception);
            }

            try
            {
                if (factory is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else if (factory is IDisposable disposable)
                    disposable.Dispose();
            }
            catch (Exception exception)
            {
                AddFailure(ref failures, exception);
            }
        }

        ThrowFailures(failures);
    }

    private static void AddFailure(ref List<Exception>? failures, Exception exception)
    {
        failures ??= [];
        failures.Add(exception);
    }

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is null)
            return;
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        throw new AggregateException("Multiple failures occurred while releasing the Quartz scheduler lease.", failures);
    }
}
