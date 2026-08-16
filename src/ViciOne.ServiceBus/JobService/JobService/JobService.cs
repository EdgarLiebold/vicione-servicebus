namespace ViciOne.ServiceBus.JobService;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration;
using Consumer;
using Contracts.JobService;
using Messages;
using Middleware;


public class JobService :
    IJobService
{
    readonly ConcurrentDictionary<Guid, JobHandle> _jobs;
    readonly Dictionary<Type, IJobTypeRegistration> _jobTypes;
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
    Heartbeat _heartbeat;

    /// <summary>Guarded by <see cref="_admission" />; never read outside it.</summary>
    bool _stopping;

    public JobService(JobServiceSettings settings)
    {
        Settings = settings;

        _jobTypes = new Dictionary<Type, IJobTypeRegistration>();
        _jobs = new ConcurrentDictionary<Guid, JobHandle>();
    }

    public JobServiceSettings Settings { get; }

    public Uri InstanceAddress => Settings.InstanceAddress;

    public bool TryGetJob(Guid jobId, out JobHandle jobReference)
    {
        return _jobs.TryGetValue(jobId, out jobReference);
    }

    public bool TryRemoveJob(Guid jobId, out JobHandle jobHandle)
    {
        var removed = _jobs.TryRemove(jobId, out jobHandle);
        if (removed)
        {
            LogContext.Debug?.Log("Removed job: {JobId} ({Status})", jobId, jobHandle.JobTask.Status);

            return true;
        }

        return false;
    }

    public async Task StartJob<T>(ConsumeContext<StartJob> context, T job, IPipe<ConsumeContext<T>> jobPipe, JobOptions<T> jobOptions)
        where T : class
    {
        var startJob = context.Message;

        if (_jobs.ContainsKey(startJob.JobId))
            throw new JobAlreadyExistsException(startJob.JobId);

        var jobContext = new ConsumeJobContext<T>(context, InstanceAddress, job, jobOptions);

        // Admission and refusal are one decision, taken under the lock that Stop also takes. Reading a
        // flag and registering afterwards left a window in which a stop could begin, find nothing to
        // drain and finish, while this job was already on its way in.
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

            await jobContext.NotifyFaulted(new JobServiceStoppingException(startJob.JobId), Settings.RejectedJobDelay);
        }
        else
        {
            LogContext.Debug?.Log("Executing job: {JobType} {JobId} ({RetryAttempt})", TypeCache<T>.ShortName, startJob.JobId,
                startJob.RetryAttempt);

            try
            {
                var jobTask = jobPipe.Send(jobContext);

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

    public async Task Stop(IPublishEndpoint publishEndpoint)
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopUnderGate(publishEndpoint).ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    async Task StopUnderGate(IPublishEndpoint publishEndpoint)
    {
        // Set before anything else and cleared only by a BusStarted that completes: while the stop
        // drains, and until a start has really succeeded, no new job is accepted.
        lock (_admission)
            _stopping = true;

        await StopHeartbeat().ConfigureAwait(false);

        await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishJobInstanceStopped(publishEndpoint))).ConfigureAwait(false);

        // Admitted but not yet registered counts as outstanding. Draining only the registered jobs
        // would let a job that was admitted a moment before the stop appear after it had finished.
        while (_jobs.IsEmpty == false || Volatile.Read(ref _admitted) > 0)
        {
            async Task CancelJob(JobHandle jobHandle)
            {
                if (!jobHandle.JobTask.IsCompleted)
                {
                    try
                    {
                        LogContext.Debug?.Log("Canceling job: {JobId}", jobHandle.JobId);

                        await jobHandle.Cancel(JobCancellationReasons.Shutdown).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LogContext.Error?.Log(ex, "Cancel job faulted: {JobId}", jobHandle.JobId);
                    }
                }

                if (TryRemoveJob(jobHandle.JobId, out _))
                    await jobHandle.DisposeAsync().ConfigureAwait(false);
            }

            await Task.WhenAll(_jobs.Values.Select(jobHandle => Task.Run(() => CancelJob(jobHandle)))).ConfigureAwait(false);

            if (_jobs.IsEmpty && Volatile.Read(ref _admitted) > 0)
                await Task.Yield();
        }
    }

    /// <summary>
    /// Ends the running heartbeat and waits for it. Called only under the lifecycle gate.
    /// <para>
    /// Waiting is the point. A timer was disposed without waiting for the publication it had already
    /// started, so a heartbeat could reach the broker after Stop had returned — the service announcing
    /// itself alive after saying it had stopped. The loop owns its own publication and is awaited here,
    /// so when this returns nothing of it is still in flight.
    /// </para>
    /// </summary>
    async Task StopHeartbeat()
    {
        var heartbeat = _heartbeat;
        if (heartbeat == null)
            return;

        _heartbeat = null;

        await heartbeat.Stop().ConfigureAwait(false);
    }

    public void RegisterJobType<T>(IReceiveEndpointConfigurator configurator, JobOptions<T> options, Guid jobTypeId, string jobTypeName)
        where T : class
    {
        if (_jobTypes.ContainsKey(typeof(T)))
            throw new ConfigurationException($"A job type can only be registered once per service instance: {TypeCache<T>.ShortName}");

        _jobTypes.Add(typeof(T), new JobTypeRegistration<T>(options, InstanceAddress, jobTypeId, jobTypeName));
    }

    public async Task BusStarted(IPublishEndpoint publishEndpoint)
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            // Whatever a previous lifecycle left behind goes first, so a successful start owns exactly
            // one heartbeat rather than adding a second one beside an older timer.
            await StopHeartbeat().ConfigureAwait(false);

            await Task.WhenAll(_jobTypes.Values.Select(x => x.PublishConcurrentJobLimit(publishEndpoint))).ConfigureAwait(false);

            // Exactly one generation per successful start: the previous one is ended and awaited above,
            // so two loops never publish side by side.
            _heartbeat = new Heartbeat(this, publishEndpoint, Settings.HeartbeatInterval);

            // Last, and only on the success path. Stop sets this and nothing cleared it again, which is
            // what left a restarted service rejecting every job for good; clearing it before the start
            // has actually completed would be the same defect with the sign reversed.
            lock (_admission)
                _stopping = false;
        }
        catch
        {
            // A start that did not complete leaves no running state and no live heartbeat behind.
            await StopHeartbeat().ConfigureAwait(false);

            lock (_admission)
                _stopping = true;

            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    Task PublishHeartbeats(IPublishEndpoint publishEndpoint)
    {
        return Task.WhenAll(_jobTypes.Values.Select(x => x.PublishHeartbeat(publishEndpoint)));
    }

    public Guid GetJobTypeId<T>()
        where T : class
    {
        if (_jobTypes.TryGetValue(typeof(T), out var registration))
            return registration.JobTypeId;

        throw new ConfigurationException($"The job type was not registered: {TypeCache<T>.ShortName}");
    }

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

        jobHandle.JobTask.ContinueWith(async innerTask =>
        {
            if (TryRemoveJob(jobHandle.JobId, out _))
                await jobHandle.DisposeAsync().ConfigureAwait(false);
        });
    }


    /// <summary>
    /// One generation of heartbeat publication, owned by the start that created it.
    /// <para>
    /// A timer plus a fire-and-forget Task.Run cannot be ended: disposing the timer stops further ticks
    /// but says nothing about the publication already running, so a heartbeat could still reach the
    /// broker after Stop had returned. This loop holds its own cancellation and its own task, so ending
    /// it is something that can be awaited — and Stop does await it.
    /// </para>
    /// <para>
    /// The interval is a delay between publications rather than a rate, so two publications of the same
    /// generation never overlap however slow the broker is.
    /// </para>
    /// </summary>
    class Heartbeat
    {
        readonly CancellationTokenSource _stopping;
        readonly Task _publishing;

        public Heartbeat(JobService service, IPublishEndpoint publishEndpoint, TimeSpan interval)
        {
            _stopping = new CancellationTokenSource();
            _publishing = Run(service, publishEndpoint, interval, _stopping.Token);
        }

        public async Task Stop()
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

        static async Task Run(JobService service, IPublishEndpoint publishEndpoint, TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    await service.PublishHeartbeats(publishEndpoint).ConfigureAwait(false);
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
        Task PublishConcurrentJobLimit(IPublishEndpoint publishEndpoint);
        Task PublishHeartbeat(IPublishEndpoint publishEndpoint);
        Task PublishJobInstanceStopped(IPublishEndpoint publishEndpoint);
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

        public Task PublishConcurrentJobLimit(IPublishEndpoint publishEndpoint)
        {
            LogContext.Debug?.Log("Job Service type: {JobType}", TypeCache<T>.ShortName);

            return PublishSetConcurrentJobLimit(publishEndpoint, ConcurrentLimitKind.Configured);
        }

        public Task PublishHeartbeat(IPublishEndpoint publishEndpoint)
        {
            return PublishSetConcurrentJobLimit(publishEndpoint, ConcurrentLimitKind.Heartbeat);
        }

        public Task PublishJobInstanceStopped(IPublishEndpoint publishEndpoint)
        {
            return PublishSetConcurrentJobLimit(publishEndpoint, ConcurrentLimitKind.Stopped);
        }

        public Guid JobTypeId { get; }

        Task PublishSetConcurrentJobLimit(IPublishEndpoint publishEndpoint, ConcurrentLimitKind kind)
        {
            return publishEndpoint.Publish<SetConcurrentJobLimit>(new SetConcurrentJobLimitCommand
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
