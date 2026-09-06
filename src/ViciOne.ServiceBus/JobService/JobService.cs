using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides the job service.</summary>
public class JobService :
    IJobService
{
    readonly ConcurrentDictionary<Guid, JobHandle> _jobs;
    readonly Dictionary<Type, IJobTypeRegistration> _jobTypes;
    readonly PendingTaskCollection _jobCompletions;
    /// <summary>
    /// The single owner of a lifecycle transition. Stop and BusStarted both move the stopping state and
    /// the heartbeat, and volatile only makes a write visible — it orders nothing. A later transition
    /// therefore has to wait for the running one, or a BusStarted that overtakes a Stop leaves a service
    /// that considers itself running while the stop is still draining jobs. A monitor lock is not an
    /// option here because both transitions await.
    /// </summary>
    readonly SemaphoreSlim _lifecycle = new SemaphoreSlim(1, 1);

    /// <summary>
    /// The admission boundary between "no more jobs" and "this job is mine".
    /// <para>
    /// The lifecycle gate above orders Stop against BusStarted, but not against a job arriving. StartJob
    /// could read a service that was not stopping, be overtaken by a Stop that drained an empty set, and
    /// register its handle afterwards — a job that outlives the stop that was supposed to end it. Both
    /// halves of that race are short and synchronous, so a monitor lock closes it: setting the stopping
    /// state, and deciding whether a job is admitted.
    /// </para>
    /// </summary>
    readonly object _admission = new object();

    /// <summary>Jobs admitted but not yet registered. Guarded by <see cref="_admission" />.</summary>
    int _admitted;

    /// <summary>The running heartbeat, or null. Only ever touched while <see cref="_lifecycle" /> is held.</summary>
    Heartbeat? _heartbeat;

    /// <summary>Guarded by <see cref="_admission" />; never read outside it.</summary>
    bool _stopping;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public JobService(JobServiceSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _jobTypes = new Dictionary<Type, IJobTypeRegistration>();
        _jobs = new ConcurrentDictionary<Guid, JobHandle>();
        _jobCompletions = new PendingTaskCollection(16);
    }

    /// <summary>Gets the settings.</summary>
    public JobServiceSettings Settings { get; }

    /// <summary>Gets the instance address.</summary>
    public Uri InstanceAddress => Settings.InstanceAddress
        ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", "The job service instance address must be configured before the service is used.", "Correct the named configuration before starting the host"));

    /// <summary>Attempts to get job.</summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="jobReference">Receives the job reference produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference)
    {
        return _jobs.TryGetValue(jobId, out jobReference);
    }

    /// <summary>Attempts to remove job.</summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="jobHandle">Receives the job handle produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle)
    {
        var removed = _jobs.TryRemove(jobId, out jobHandle);
        if (removed && jobHandle != null)
        {
            LogContext.Debug?.Log("Removed job: {JobId} ({Status})", jobId, jobHandle.JobTask.Status);

            return true;
        }

        return false;
    }

    /// <summary>Starts job.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="job">The job.</param>
    /// <param name="jobPipe">The job pipe.</param>
    /// <param name="jobOptions">The job options.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartJobAsync<T>(ConsumeContext<StartJob> context, T job, IPipe<ConsumeContext<T>> jobPipe, JobOptions<T> jobOptions, CancellationToken cancellationToken = default)
        where T : class
    {
        var startJob = context.Message;

        if (_jobs.ContainsKey(startJob.JobId))
            throw new JobAlreadyExistsException(startJob.JobId);

        var jobContext = new ConsumeJobContext<T>(context, InstanceAddress, job, jobOptions);

        // Admission and refusal share the lock used by Stop. No job can be admitted after shutdown
        // begins, and every admitted job is included in the shutdown drain.
        var admitted = false;

        lock (_admission)
        {
            if (!_stopping)
            {
                _admitted++;
                admitted = true;
            }
        }

        if (!admitted)
        {
            LogContext.Debug?.Log("Rejecting job: {JobType} {JobId} ({RetryAttempt}) - Job Service is stopping", TypeCache<T>.ShortName, startJob.JobId,
                startJob.RetryAttempt);

            await jobContext.NotifyFaultedAsync(new JobServiceStoppingException(startJob.JobId), Settings.RejectedJobDelay, cancellationToken: cancellationToken);
        }
        else
        {
            LogContext.Debug?.Log("Executing job: {JobType} {JobId} ({RetryAttempt})", TypeCache<T>.ShortName, startJob.JobId,
                startJob.RetryAttempt);

            try
            {
                var jobTask = jobPipe.SendAsync(jobContext);

                var jobHandle = new ConsumerJobHandle<T>(jobContext, jobTask, jobOptions.JobCancellationTimeout);

                Add(jobHandle);
            }
            finally
            {
                // Released only once the handle is registered, or once registering has failed. A stop
                // running in parallel waits for this count, so the job is never invisible to it.
                lock (_admission)
                    _admitted--;
            }
        }
    }

    /// <summary>Stops the configured component.</summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopUnderGateAsync(publishEndpoint).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    async Task StopUnderGateAsync(IPublishEndpoint publishEndpoint)
    {
        // Set before anything else and cleared only by a BusStarted that completes: while the stop
        // drains, and until a start has really succeeded, no new job is accepted.
        lock (_admission)
            _stopping = true;

        await StopHeartbeatAsync().ConfigureAwait(false);

        await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishJobInstanceStoppedAsync(publishEndpoint))).ConfigureAwait(false);

        // Admitted but not yet registered counts as outstanding. Draining only the registered jobs
        // would let a job that was admitted a moment before the stop appear after it had finished.
        while (_jobs.IsEmpty == false || Volatile.Read(ref _admitted) > 0)
        {
            async Task CancelJobAsync(JobHandle jobHandle)
            {
                if (!jobHandle.JobTask.IsCompleted)
                {
                    try
                    {
                        LogContext.Debug?.Log("Canceling job: {JobId}", jobHandle.JobId);

                        await jobHandle.CancelAsync(JobCancellationReasons.Shutdown).ConfigureAwait(false);
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

            if (_jobs.IsEmpty && Volatile.Read(ref _admitted) > 0)
                await Task.Yield();
        }

        await _jobCompletions.CompletedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the running heartbeat and waits until its publication loop has finished. The lifecycle
    /// gate is held by the caller, and no heartbeat publication remains in flight when this method returns.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    async Task StopHeartbeatAsync()
    {
        var heartbeat = _heartbeat;
        if (heartbeat == null)
            return;

        _heartbeat = null;

        await heartbeat.StopAsync().ConfigureAwait(false);
    }

    /// <summary>Registers job type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="jobTypeId">The job type id.</param>
    /// <param name="jobTypeName">The job type name.</param>
    public void RegisterJobType<T>(IReceiveEndpointConfigurator configurator, JobOptions<T> options, Guid jobTypeId, string jobTypeName)
        where T : class
    {
        if (_jobTypes.ContainsKey(typeof(T)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", $"A job type can only be registered once per service instance: {TypeCache<T>.ShortName}", "Correct the named configuration before starting the host"));

        _jobTypes.Add(typeof(T), new JobTypeRegistration<T>(options, InstanceAddress, jobTypeId, jobTypeName));
    }

    /// <summary>Notifies the component that the bus has started.</summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            // Each successful lifecycle owns exactly one heartbeat loop.
            await StopHeartbeatAsync().ConfigureAwait(false);

            await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishConcurrentJobLimitAsync(publishEndpoint))).ConfigureAwait(false);

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

    Task PublishHeartbeatsAsync(IPublishEndpoint publishEndpoint)
    {
        return Task.WhenAll(_jobTypes.Values.Select(x => x.PublishHeartbeatAsync(publishEndpoint)));
    }

    /// <summary>Gets job type id.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The job type id.</returns>
    public Guid GetJobTypeId<T>()
        where T : class
    {
        if (_jobTypes.TryGetValue(typeof(T), out var registration))
            return registration.JobTypeId;

        throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", $"The job type was not registered: {TypeCache<T>.ShortName}", "Correct the named configuration before starting the host"));
    }

    /// <summary>Configures supervise job consumer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator)
    {
        var partition = new Middleware.Partitioner(16, new Murmur3UnsafeHashGenerator());

        configurator.UsePartitioner<CancelJobAttempt>(partition, p => p.Message.JobId);
        configurator.UsePartitioner<GetJobAttemptStatus>(partition, p => p.Message.JobId);

        var consumerFactory = new DelegateConsumerFactory<SuperviseJobConsumer>(() => new SuperviseJobConsumer(this));

        var consumerConfigurator = new ConsumerConfigurator<SuperviseJobConsumer>(consumerFactory, configurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    void Add(JobHandle jobHandle)
    {
        if (!_jobs.TryAdd(jobHandle.JobId, jobHandle))
            throw new JobAlreadyExistsException(jobHandle.JobId);

        _jobCompletions.Add(CompleteJobAsync(jobHandle));
    }

    async Task CompleteJobAsync(JobHandle jobHandle)
    {
        try
        {
            await jobHandle.JobTask.ConfigureAwait(false);
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
    class Heartbeat
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
            catch (OperationCanceledException)
            {
                // The loop ended because it was asked to.
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
                    await service.PublishHeartbeatsAsync(publishEndpoint).ConfigureAwait(false);
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
        Task PublishConcurrentJobLimitAsync(IPublishEndpoint publishEndpoint);
        Task PublishHeartbeatAsync(IPublishEndpoint publishEndpoint);
        Task PublishJobInstanceStoppedAsync(IPublishEndpoint publishEndpoint);
    }


    class JobTypeRegistration<T> :
        IJobTypeRegistration
        where T : class
    {
        readonly Uri _instanceAddress;
        readonly JobOptions<T> _options;

        public JobTypeRegistration(JobOptions<T> options, Uri instanceAddress, Guid jobTypeId, string jobTypeName)
        {
            _options = options;
            _instanceAddress = instanceAddress;
            JobTypeId = jobTypeId;
            JobTypeName = string.IsNullOrWhiteSpace(options.JobTypeName) ? jobTypeName : options.JobTypeName;
        }

        string JobTypeName { get; }

        public Task PublishConcurrentJobLimitAsync(IPublishEndpoint publishEndpoint)
        {
            LogContext.Debug?.Log("Job Service type: {JobType}", TypeCache<T>.ShortName);

            return PublishSetConcurrentJobLimitAsync(publishEndpoint, ConcurrentLimitKind.Configured);
        }

        public Task PublishHeartbeatAsync(IPublishEndpoint publishEndpoint)
        {
            return PublishSetConcurrentJobLimitAsync(publishEndpoint, ConcurrentLimitKind.Heartbeat);
        }

        public Task PublishJobInstanceStoppedAsync(IPublishEndpoint publishEndpoint)
        {
            return PublishSetConcurrentJobLimitAsync(publishEndpoint, ConcurrentLimitKind.Stopped);
        }

        public Guid JobTypeId { get; }

        Task PublishSetConcurrentJobLimitAsync(IPublishEndpoint publishEndpoint, ConcurrentLimitKind kind)
        {
            return publishEndpoint.PublishAsync<SetConcurrentJobLimit>(new SetConcurrentJobLimitCommand
            {
                JobTypeId = JobTypeId,
                JobTypeName = JobTypeName,
                InstanceAddress = _instanceAddress,
                ConcurrentJobLimit = _options.ConcurrentJobLimit,
                Kind = kind,
                JobTypeProperties = _options.JobTypeProperties.Properties,
                InstanceProperties = _options.InstanceProperties.Properties,
                GlobalConcurrentJobLimit = _options.GlobalConcurrentJobLimit
            });
        }
    }
}
