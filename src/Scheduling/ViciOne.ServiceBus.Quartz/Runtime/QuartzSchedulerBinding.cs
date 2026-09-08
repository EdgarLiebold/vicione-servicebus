using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Quartz.Configuration;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Owns the scheduler identity, lifecycle state and immutable settings for one bus.</summary>
internal sealed class QuartzSchedulerBinding<TBus> : IAsyncDisposable
    where TBus : class, IBus
{
    readonly QuartzSchedulerClaimRegistry _claims;
    readonly ISchedulerFactory _schedulerFactory;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly bool _ownsFactory;
    readonly TimeProvider _timeProvider;
    bool _attached;
    bool _disposed;
    IBus? _attachedBus;
    IScheduler? _scheduler;

    public QuartzSchedulerBinding(
        ISchedulerFactory schedulerFactory,
        bool ownsFactory,
        QuartzEndpointSettings settings,
        TimeProvider timeProvider,
        QuartzSchedulerClaimRegistry claims)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _claims = claims ?? throw new ArgumentNullException(nameof(claims));
        _ownsFactory = ownsFactory;
        BusKey = typeof(TBus).FullName ?? typeof(TBus).Name;
        _claims.ClaimFactory(_schedulerFactory, BusKey);
    }

    public string BusKey { get; }

    public QuartzEndpointSettings Settings { get; }

    public async ValueTask<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            return await GetSchedulerCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            IScheduler scheduler = await GetSchedulerCoreAsync().ConfigureAwait(false);
            Attach(scheduler, bus, _timeProvider);
            _attachedBus = bus;
            _attached = true;

            try
            {
                if (Settings.StartDelay.HasValue)
                    await scheduler.StartDelayed(Settings.StartDelay.Value).ConfigureAwait(false);
                else
                    await scheduler.Start().ConfigureAwait(false);
            }
            catch
            {
                Detach(scheduler, bus);
                _attachedBus = null;
                _attached = false;
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StandbyAndDetachAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_scheduler is null || !_attached)
                return;

            try
            {
                await _scheduler.Standby().ConfigureAwait(false);
            }
            finally
            {
                Detach(_scheduler, _attachedBus);
                _attachedBus = null;
                _attached = false;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
                return;

            _disposed = true;
            List<Exception>? failures = null;
            if (_scheduler is not null)
            {
                try
                {
                    if (_ownsFactory)
                    {
                        if (_scheduler.Status != SchedulerStatus.Shutdown)
                            await _scheduler.Shutdown(Settings.WaitForJobsToComplete).ConfigureAwait(false);
                    }
                    else if (_attached)
                        await _scheduler.Standby().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    AddFailure(ref failures, exception);
                }
                finally
                {
                    Detach(_scheduler, _attachedBus);
                    _attachedBus = null;
                    _attached = false;
                }
            }

            if (_ownsFactory)
            {
                try
                {
                    if (_schedulerFactory is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    else if (_schedulerFactory is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception exception)
                {
                    AddFailure(ref failures, exception);
                }
            }

            ThrowFailures(failures);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<IScheduler> GetSchedulerCoreAsync(CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = _scheduler
            ?? await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
        if (scheduler.Status == SchedulerStatus.Shutdown)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Quartz scheduling",
                BusKey,
                "The assigned scheduler has been shut down and cannot be restarted",
                "Provide a live scheduler factory or configure an adapter-owned in-memory scheduler"));
        }

        _claims.ClaimScheduler(scheduler, BusKey);
        _scheduler = scheduler;
        return scheduler;
    }

    private void Attach(IScheduler scheduler, IBus bus, TimeProvider timeProvider)
    {
        lock (scheduler.Context)
        {
            if (scheduler.Context.TryGetValue(QuartzSchedulerContextKeys.Bus, out object? owner)
                && !ReferenceEquals(owner, bus))
            {
                throw new ConfigurationException(ConfigurationMessages.Create(
                    "Quartz scheduling",
                    BusKey,
                    $"Scheduler '{scheduler.SchedulerName}' is already attached to another bus",
                    "Provide a distinct scheduler factory for each bus"));
            }

            scheduler.Context[QuartzSchedulerContextKeys.Bus] = bus;
            scheduler.Context[QuartzSchedulerContextKeys.TimeProvider] = timeProvider;
        }
    }

    private static void Detach(IScheduler scheduler, IBus? bus)
    {
        lock (scheduler.Context)
        {
            if (!scheduler.Context.TryGetValue(QuartzSchedulerContextKeys.Bus, out object? owner)
                || !ReferenceEquals(owner, bus))
            {
                return;
            }

            scheduler.Context.Remove(QuartzSchedulerContextKeys.Bus);
            scheduler.Context.Remove(QuartzSchedulerContextKeys.TimeProvider);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
        throw new AggregateException("Multiple failures occurred while releasing the Quartz scheduler binding.", failures);
    }
}
