using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coordinates job admission, local execution ownership, lifecycle transitions, and instance heartbeats.</summary>
/// <param name="settings">The runtime settings owned by the service instance.</param>
internal sealed class JobService(IJobServiceSettings settings) :
    IJobService
{
    readonly ConcurrentDictionary<Guid, IJobHandle> _jobs = [];
    readonly Dictionary<Type, IJobTypeRegistration> _jobTypes = [];
    readonly PendingTaskCollection _jobCompletions = new(16);
    readonly HashSet<Guid> _reservedJobIds = [];

    /// <summary>Serializes asynchronous start and stop transitions.</summary>
    readonly SemaphoreSlim _lifecycle = new(1, 1);

    /// <summary>Guards admission state and job identifiers between reservation and handle registration.</summary>
    readonly Lock _admission = new();

    /// <summary>Signals when all admitted jobs have either registered a handle or failed to start.</summary>
    TaskCompletionSource? _admissionDrained;

    /// <summary>Counts jobs admitted but not yet registered; guarded by <see cref="_admission" />.</summary>
    int _admitted;

    /// <summary>References the active heartbeat generation while the lifecycle gate is held.</summary>
    Heartbeat? _heartbeat;

    /// <summary>Prevents admission during and after shutdown; guarded by <see cref="_admission" />.</summary>
    bool _stopping;

    /// <inheritdoc />
    public IJobServiceSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <inheritdoc />
    public Uri InstanceAddress => Settings.InstanceAddress
        ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", "The job service instance address must be configured before the service is used.", "Correct the named configuration before starting the host"));

    /// <inheritdoc />
    public bool TryGetJob(Guid jobId, [NotNullWhen(true)] out IJobHandle? jobReference)
    {
        return _jobs.TryGetValue(jobId, out jobReference);
    }

    /// <inheritdoc />
    public bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out IJobHandle? jobHandle)
    {
        var removed = _jobs.TryRemove(jobId, out jobHandle);
        if (removed && jobHandle != null)
        {
            LogContext.Debug?.Log("Removed job: {JobId} ({Status})", jobId, jobHandle.Execution.Status);

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task StartJobAsync<TJob>(
        ConsumeContext<IStartJob> context,
        TJob job,
        IPipe<ConsumeContext<TJob>> jobPipe,
        JobOptions<TJob> jobOptions,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(jobPipe);
        ArgumentNullException.ThrowIfNull(jobOptions);
        cancellationToken.ThrowIfCancellationRequested();

        var startJob = context.Message;
        var admitted = false;

        lock (_admission)
        {
            if (!_stopping)
            {
                if (_jobs.ContainsKey(startJob.JobId) || !_reservedJobIds.Add(startJob.JobId))
                    throw new JobAlreadyExistsException(startJob.JobId);

                if (_admitted == 0)
                    _admissionDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                _admitted++;
                admitted = true;
            }
        }

        if (!admitted)
        {
            LogContext.Debug?.Log("Rejecting job: {JobType} {JobId} ({RetryAttempt}) - Job Service is stopping", TypeCache<TJob>.ShortName, startJob.JobId,
                startJob.RetryAttempt);

            await using var rejectedContext = new ConsumeJobContext<TJob>(context, InstanceAddress, job, jobOptions);
            await rejectedContext.NotifyFaultedAsync(
                new JobServiceStoppingException(startJob.JobId),
                Settings.RejectedJobDelay,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return;
        }

        LogContext.Debug?.Log("Executing job: {JobType} {JobId} ({RetryAttempt})", TypeCache<TJob>.ShortName, startJob.JobId,
            startJob.RetryAttempt);

        ConsumeJobContext<TJob>? jobContext = null;
        var handleOwnsContext = false;
        try
        {
            TimeSpan jobCancellationTimeout = jobOptions.JobCancellationTimeout;
            jobContext = new ConsumeJobContext<TJob>(context, InstanceAddress, job, jobOptions);
            var jobTask = jobPipe.SendAsync(jobContext);
            var jobHandle = new ConsumerJobHandle<TJob>(jobContext, jobTask, jobCancellationTimeout);

            Add(jobHandle);
            handleOwnsContext = true;
        }
        finally
        {
            lock (_admission)
            {
                _reservedJobIds.Remove(startJob.JobId);
                _admitted--;
                if (_admitted == 0)
                    _admissionDrained?.TrySetResult();
            }

            if (jobContext is not null && !handleOwnsContext)
                await jobContext.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopUnderGateAsync(publishEndpoint, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    async Task StopUnderGateAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        Task admissionDrained;
        lock (_admission)
        {
            _stopping = true;
            admissionDrained = _admitted == 0
                ? Task.CompletedTask
                : (_admissionDrained ?? throw new InvalidOperationException("Admitted jobs require a drain signal.")).Task;
        }

        await admissionDrained.ConfigureAwait(false);

        await StopHeartbeatAsync().ConfigureAwait(false);

        await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishJobInstanceStoppedAsync(publishEndpoint, cancellationToken))).ConfigureAwait(false);

        async Task CancelJobAsync(IJobHandle jobHandle)
        {
            if (!jobHandle.Execution.IsCompleted)
            {
                try
                {
                    LogContext.Debug?.Log("Canceling job: {JobId}", jobHandle.JobId);

                    await jobHandle.CancelAsync(JobCancellationReasons.Shutdown, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogContext.Error?.Log(ex, "Cancel job faulted: {JobId}", jobHandle.JobId);
                }
            }

            if (TryRemoveJob(jobHandle.JobId, out _))
                await jobHandle.DisposeAsync().ConfigureAwait(false);
        }

        await Task.WhenAll(_jobs.Values.Select(CancelJobAsync)).ConfigureAwait(false);
        await _jobCompletions.CompletedAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the running heartbeat and waits until its publication loop has finished. The lifecycle
    /// gate is held by the caller, and no heartbeat publication remains in flight when this method returns.
    /// </summary>
    /// <returns>A task that completes after the active heartbeat generation stops.</returns>
    async Task StopHeartbeatAsync()
    {
        var heartbeat = _heartbeat;
        if (heartbeat == null)
            return;

        _heartbeat = null;

        await heartbeat.StopAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RegisterJobType<TJob>(JobOptions<TJob> options, Guid jobTypeId, string jobTypeName)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobTypeName);
        if (jobTypeId == Guid.Empty)
            throw new ArgumentException("The job type identifier cannot be empty.", nameof(jobTypeId));

        if (_jobTypes.ContainsKey(typeof(TJob)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", $"A job type can only be registered once per service instance: {TypeCache<TJob>.ShortName}", "Correct the named configuration before starting the host"));

        _jobTypes.Add(typeof(TJob), new JobTypeRegistration<TJob>(options, InstanceAddress, jobTypeId, jobTypeName));
    }

    /// <inheritdoc />
    public async Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Each successful lifecycle owns exactly one heartbeat loop.
            await StopHeartbeatAsync().ConfigureAwait(false);

            await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishConcurrentJobLimitAsync(publishEndpoint, cancellationToken))).ConfigureAwait(false);

            // Create the heartbeat only after the prior loop has stopped completely.
            _heartbeat = new Heartbeat(this, publishEndpoint, Settings.HeartbeatInterval, Settings.TimeProvider);

            // Admission opens only after every startup action has completed successfully.
            lock (_admission)
                _stopping = false;
        }
        catch
        {
            // Failed startup leaves neither running state nor a live heartbeat.
            await StopHeartbeatAsync().ConfigureAwait(false);

            lock (_admission)
                _stopping = true;

            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    Task PublishHeartbeatsAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        return Task.WhenAll(_jobTypes.Values.Select(x => x.PublishHeartbeatAsync(publishEndpoint, cancellationToken)));
    }

    /// <inheritdoc />
    public Guid GetJobTypeId<TJob>()
        where TJob : class
    {
        if (_jobTypes.TryGetValue(typeof(TJob), out var registration))
            return registration.JobTypeId;

        throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", $"The job type was not registered: {TypeCache<TJob>.ShortName}", "Correct the named configuration before starting the host"));
    }

    /// <inheritdoc />
    public void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        var partition = new PartitionCoordinator(16);

        configurator.UsePartitioner<ICancelJobAttempt>(partition, p => p.Message.JobId);
        configurator.UsePartitioner<IGetJobAttemptStatus>(partition, p => p.Message.JobId);

        var consumerFactory = new DelegateConsumerFactory<SuperviseJobConsumer>(() => new SuperviseJobConsumer(this));

        var consumerConfigurator = new ConsumerConfigurator<SuperviseJobConsumer>(consumerFactory, configurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    void Add<TJob>(ConsumerJobHandle<TJob> jobHandle)
        where TJob : class
    {
        if (!_jobs.TryAdd(jobHandle.JobId, jobHandle))
            throw new JobAlreadyExistsException(jobHandle.JobId);

        _jobCompletions.Add(CompleteJobAsync(jobHandle));
    }

    async Task CompleteJobAsync<TJob>(ConsumerJobHandle<TJob> jobHandle)
        where TJob : class
    {
        try
        {
            await jobHandle.Completion.ConfigureAwait(false);
        }
        catch
        {
            // The consumer pipeline reports the job failure. This owner observes terminal state so
            // cleanup is deterministic and no faulted task remains detached.
        }

        if (TryRemoveJob(jobHandle.JobId, out _))
            await jobHandle.DisposeAsync().ConfigureAwait(false);
    }


    /// <summary>
    /// Owns one cancellable heartbeat publication loop and exposes its completion to the enclosing service.
    /// <para>
    /// The interval is a delay between publications, so publications from the same generation never overlap.
    /// </para>
    /// </summary>
    sealed class Heartbeat
    {
        readonly CancellationTokenSource _stopping;
        readonly Task _publishing;

        public Heartbeat(JobService service, IPublishEndpoint publishEndpoint, TimeSpan interval, TimeProvider timeProvider)
        {
            _stopping = new CancellationTokenSource();
            _publishing = RunAsync(service, publishEndpoint, interval, timeProvider, _stopping.Token);
        }

        public async Task StopAsync()
        {
            _stopping.Cancel();

            try
            {
                await _publishing.ConfigureAwait(false);
            }
            finally
            {
                _stopping.Dispose();
            }
        }

        static async Task RunAsync(JobService service, IPublishEndpoint publishEndpoint, TimeSpan interval, TimeProvider timeProvider,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    await service.PublishHeartbeatsAsync(publishEndpoint, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    LogContext.Debug?.Log(exception, "Failed to publish heartbeat");
                }
            }
        }
    }


    interface IJobTypeRegistration
    {
        Guid JobTypeId { get; }
        Task PublishConcurrentJobLimitAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken);
        Task PublishHeartbeatAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken);
        Task PublishJobInstanceStoppedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken);
    }


    sealed class JobTypeRegistration<TJob>(JobOptions<TJob> options, Uri instanceAddress, Guid jobTypeId, string jobTypeName) :
        IJobTypeRegistration
        where TJob : class
    {
        readonly Uri _instanceAddress = instanceAddress;
        readonly int _concurrentJobLimit = options.ConcurrentJobLimit;
        readonly int? _globalConcurrentJobLimit = options.GlobalConcurrentJobLimit;
        readonly IReadOnlyDictionary<string, object> _instanceProperties = JobPropertySnapshot.Create(options.InstancePropertyValues);
        readonly IReadOnlyDictionary<string, object> _jobTypeProperties = JobPropertySnapshot.Create(options.JobTypePropertyValues);

        string JobTypeName { get; } = string.IsNullOrWhiteSpace(options.JobTypeName) ? jobTypeName : options.JobTypeName;

        public Task PublishConcurrentJobLimitAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
        {
            LogContext.Debug?.Log("Job Service type: {JobType}", TypeCache<TJob>.ShortName);

            return PublishSetConcurrentJobLimitAsync(publishEndpoint, JobConcurrencyUpdateKind.Configuration, cancellationToken);
        }

        public Task PublishHeartbeatAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
        {
            return PublishSetConcurrentJobLimitAsync(publishEndpoint, JobConcurrencyUpdateKind.Heartbeat, cancellationToken);
        }

        public Task PublishJobInstanceStoppedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
        {
            return PublishSetConcurrentJobLimitAsync(publishEndpoint, JobConcurrencyUpdateKind.InstanceStopped, cancellationToken);
        }

        public Guid JobTypeId { get; } = jobTypeId;

        Task PublishSetConcurrentJobLimitAsync(
            IPublishEndpoint publishEndpoint,
            JobConcurrencyUpdateKind updateKind,
            CancellationToken cancellationToken)
        {
            return publishEndpoint.PublishAsync<ISetConcurrentJobLimit>(new SetConcurrentJobLimitCommand
            {
                JobTypeId = JobTypeId,
                JobTypeName = JobTypeName,
                InstanceAddress = _instanceAddress,
                ConcurrentJobLimit = _concurrentJobLimit,
                UpdateKind = updateKind,
                JobTypeProperties = _jobTypeProperties,
                InstanceProperties = _instanceProperties,
                GlobalConcurrentJobLimit = _globalConcurrentJobLimit
            }, cancellationToken);
        }
    }
}
