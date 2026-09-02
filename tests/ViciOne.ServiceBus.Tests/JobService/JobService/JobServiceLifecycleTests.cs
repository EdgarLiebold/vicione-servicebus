using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using RuntimeJobService = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobServiceLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "bidirectional-transition-serialization")]
    public async Task StartAndStopTransitions_AreSerializedInBothDirections()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var stopFirstEndpoint = new ControlledPublishEndpoint();
        PublicationGate stopped = stopFirstEndpoint.BlockNext(ConcurrentLimitKind.Stopped);
        RuntimeJobService stopFirstService = NewService(TimeSpan.FromDays(1));

        Task stopping = stopFirstService.Stop(stopFirstEndpoint);
        await stopped.Entered.WaitAsync(timeout, cancellationToken);
        Task starting = stopFirstService.BusStarted(stopFirstEndpoint);

        Assert.False(starting.IsCompleted);
        Assert.Equal(0, stopFirstEndpoint.Count(ConcurrentLimitKind.Configured));

        stopped.Release();
        await Task.WhenAll(stopping, starting).WaitAsync(timeout, cancellationToken);
        await stopFirstService.Stop(stopFirstEndpoint).WaitAsync(timeout, cancellationToken);

        var startFirstEndpoint = new ControlledPublishEndpoint();
        PublicationGate configured = startFirstEndpoint.BlockNext(ConcurrentLimitKind.Configured);
        RuntimeJobService startFirstService = NewService(TimeSpan.FromDays(1));

        starting = startFirstService.BusStarted(startFirstEndpoint);
        await configured.Entered.WaitAsync(timeout, cancellationToken);
        stopping = startFirstService.Stop(startFirstEndpoint);

        Assert.False(stopping.IsCompleted);
        Assert.Equal(0, startFirstEndpoint.Count(ConcurrentLimitKind.Stopped));

        configured.Release();
        await Task.WhenAll(starting, stopping).WaitAsync(timeout, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "stop-drains-heartbeat-and-leaves-no-generation")]
    public async Task Stop_DrainsTheExactHeartbeatGenerationBeforeReturning()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        PublicationGate heartbeat = endpoint.BlockNext(ConcurrentLimitKind.Heartbeat);
        RuntimeJobService service = NewService(TimeSpan.Zero);

        await service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        await heartbeat.Entered.WaitAsync(timeout, cancellationToken);

        Task stopping = service.Stop(endpoint);

        Assert.False(stopping.IsCompleted);
        Assert.Equal(0, endpoint.Count(ConcurrentLimitKind.Stopped));

        heartbeat.Release();
        await stopping.WaitAsync(timeout, cancellationToken);

        Assert.Equal(1, endpoint.Count(ConcurrentLimitKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(ConcurrentLimitKind.Stopped));
        Assert.Equal(0, endpoint.ActiveHeartbeatPublications);
        Assert.Equal(1, endpoint.MaximumConcurrentHeartbeatPublications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "repeated-start-replaces-heartbeat-generation")]
    public async Task RepeatedStarts_ReplaceRatherThanOverlapHeartbeatGenerations()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        RuntimeJobService service = NewService(TimeSpan.Zero);

        PublicationGate first = endpoint.BlockNext(ConcurrentLimitKind.Heartbeat);
        await service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        await first.Entered.WaitAsync(timeout, cancellationToken);

        PublicationGate second = endpoint.BlockNext(ConcurrentLimitKind.Heartbeat);
        Task secondStart = service.BusStarted(endpoint);
        Assert.False(secondStart.IsCompleted);
        first.Release();
        await secondStart.WaitAsync(timeout, cancellationToken);
        await second.Entered.WaitAsync(timeout, cancellationToken);

        PublicationGate third = endpoint.BlockNext(ConcurrentLimitKind.Heartbeat);
        Task thirdStart = service.BusStarted(endpoint);
        Assert.False(thirdStart.IsCompleted);
        second.Release();
        await thirdStart.WaitAsync(timeout, cancellationToken);
        await third.Entered.WaitAsync(timeout, cancellationToken);

        Task stopping = service.Stop(endpoint);
        Assert.False(stopping.IsCompleted);
        third.Release();
        await stopping.WaitAsync(timeout, cancellationToken);

        Assert.Equal(3, endpoint.Count(ConcurrentLimitKind.Configured));
        Assert.Equal(3, endpoint.Count(ConcurrentLimitKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(ConcurrentLimitKind.Stopped));
        Assert.Equal(1, endpoint.MaximumConcurrentHeartbeatPublications);
        Assert.Equal(0, endpoint.ActiveHeartbeatPublications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "failed-start-has-no-heartbeat-and-recovers")]
    public async Task FailedStart_LeavesNoHeartbeatAndDoesNotStrandTheLifecycleGate()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        RuntimeJobService service = NewService(TimeSpan.Zero);
        var expected = new InvalidOperationException("configured announcement refused");
        endpoint.FailNext(ConcurrentLimitKind.Configured, expected);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BusStarted(endpoint));

        Assert.Same(expected, actual);
        Assert.Equal(0, endpoint.Count(ConcurrentLimitKind.Heartbeat));

        PublicationGate heartbeat = endpoint.BlockNext(ConcurrentLimitKind.Heartbeat);
        await service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        await heartbeat.Entered.WaitAsync(timeout, cancellationToken);
        Task stopping = service.Stop(endpoint);
        heartbeat.Release();
        await stopping.WaitAsync(timeout, cancellationToken);

        Assert.Equal(2, endpoint.Count(ConcurrentLimitKind.Configured));
        Assert.Equal(1, endpoint.Count(ConcurrentLimitKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(ConcurrentLimitKind.Stopped));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "complete-start-stop-failure-admission-contract")]
    public async Task Admission_FollowsTheCompleteSuccessfulAndFailedLifecycleSequence()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AdmissionFixture fixture = await AdmissionFixture.Start(timeout, cancellationToken);
        var endpoint = new ControlledPublishEndpoint();

        await fixture.Service.Stop(endpoint).WaitAsync(timeout, cancellationToken);
        var failedStart = new InvalidOperationException("first start refused");
        endpoint.FailNext(ConcurrentLimitKind.Configured, failedStart);
        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BusStarted(endpoint));
        Assert.Same(failedStart, actual);
        Assert.False(await fixture.Submit(cancellationToken));

        await fixture.Service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.Submit(cancellationToken));

        PublicationGate stopped = endpoint.BlockNext(ConcurrentLimitKind.Stopped);
        Task stopping = fixture.Service.Stop(endpoint);
        await stopped.Entered.WaitAsync(timeout, cancellationToken);
        Assert.False(await fixture.Submit(cancellationToken));
        stopped.Release();
        await stopping.WaitAsync(timeout, cancellationToken);
        Assert.False(await fixture.Submit(cancellationToken));

        await fixture.Service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.Submit(cancellationToken));
        var failedRestart = new InvalidOperationException("restart refused");
        endpoint.FailNext(ConcurrentLimitKind.Configured, failedRestart);
        actual = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BusStarted(endpoint));
        Assert.Same(failedRestart, actual);
        Assert.False(await fixture.Submit(cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "stop-waits-for-admitted-job-registration")]
    public async Task Stop_WaitsForAnAdmittedJobUntilItsHandleIsRegistered()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AdmissionFixture fixture = await AdmissionFixture.Start(timeout, cancellationToken);
        var endpoint = new ControlledPublishEndpoint();
        await fixture.Service.Stop(endpoint).WaitAsync(timeout, cancellationToken);
        await fixture.Service.BusStarted(endpoint).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.Submit(cancellationToken));

        fixture.JobPipe.HoldNext();
        Task<bool> submitting = fixture.Submit(cancellationToken);
        await fixture.JobPipe.Entered.WaitAsync(timeout, cancellationToken);

        Task stopping = fixture.Service.Stop(endpoint);

        Assert.False(stopping.IsCompleted);

        fixture.JobPipe.Release();
        Assert.True(await submitting.WaitAsync(timeout, cancellationToken));
        await stopping.WaitAsync(timeout, cancellationToken);
    }

    private static RuntimeJobService NewService(TimeSpan heartbeatInterval)
    {
        var service = new RuntimeJobService(new StubJobServiceSettings(heartbeatInterval));
        service.RegisterJobType<LifecycleJob>(null!, new JobOptions<LifecycleJob>(), NewId.NextGuid(), "lifecycle-job");
        return service;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class StubJobServiceSettings(TimeSpan heartbeatInterval) : JobServiceSettings
    {
        public IJobService JobService => throw new NotSupportedException("The lifecycle tests drive the service directly.");
        public TimeSpan HeartbeatInterval { get; } = heartbeatInterval;
        public TimeSpan RejectedJobDelay { get; } = TimeSpan.Zero;
        public Uri InstanceAddress { get; } = new("loopback://localhost/job-instance");
        public IReceiveEndpointConfigurator InstanceEndpointConfigurator => null!;

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }

    private sealed record LifecycleJob;

    private sealed class AdmissionFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly ITestHarness _harness;
        private readonly TimeSpan _timeout;

        private AdmissionFixture(
            ServiceProvider provider,
            ITestHarness harness,
            RuntimeJobService service,
            CountingJobPipe jobPipe,
            TimeSpan timeout)
        {
            _provider = provider;
            _harness = harness;
            Service = service;
            JobPipe = jobPipe;
            _timeout = timeout;
        }

        public RuntimeJobService Service { get; }
        public CountingJobPipe JobPipe { get; }

        public static async Task<AdmissionFixture> Start(TimeSpan timeout, CancellationToken cancellationToken)
        {
            RuntimeJobService service = NewService(TimeSpan.FromDays(1));
            var jobPipe = new CountingJobPipe();
            var services = new ServiceCollection();
            services.AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<StartJob>(async context =>
                {
                    TaskCompletionSource? handled = jobPipe.Handled;
                    try
                    {
                        await service.StartJob(context, new LifecycleJob(), jobPipe, new JobOptions<LifecycleJob>());
                    }
                    finally
                    {
                        handled?.TrySetResult();
                    }
                });
            });
            ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
            try
            {
                ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
                return new AdmissionFixture(provider, harness, service, jobPipe, timeout);
            }
            catch
            {
                await provider.DisposeAsync();
                jobPipe.Dispose();
                throw;
            }
        }

        public async Task<bool> Submit(CancellationToken cancellationToken)
        {
            int before = JobPipe.Count;
            var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobPipe.Handled = handled;
            ISendEndpoint handlerEndpoint = await _harness.GetHandlerEndpoint<StartJob>();
            await handlerEndpoint.Send<StartJob>(new
            {
                JobId = NewId.NextGuid(),
                AttemptId = NewId.NextGuid(),
                RetryAttempt = 0,
                Job = new Dictionary<string, object>(),
                JobTypeId = NewId.NextGuid(),
                JobProperties = new Dictionary<string, object>(),
            }, cancellationToken).WaitAsync(_timeout, cancellationToken);
            await handled.Task.WaitAsync(_timeout, cancellationToken);
            return JobPipe.Count > before;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                JobPipe.Release();
                await _harness.Stop(CancellationToken.None).WaitAsync(_timeout, CancellationToken.None);
            }
            finally
            {
                JobPipe.Dispose();
                await _provider.DisposeAsync();
            }
        }
    }

    private sealed class CountingJobPipe : IPipe<ConsumeContext<LifecycleJob>>, IDisposable
    {
        private readonly object _lock = new();
        private ManualResetEventSlim? _release;
        private TaskCompletionSource? _entered;
        private int _armed;
        private int _count;

        public int Count => Volatile.Read(ref _count);
        public TaskCompletionSource? Handled { get; set; }
        public Task Entered => _entered?.Task ?? Task.CompletedTask;

        public void HoldNext()
        {
            lock (_lock)
            {
                _entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _release?.Dispose();
                _release = new ManualResetEventSlim(false);
                Interlocked.Exchange(ref _armed, 1);
            }
        }

        public void Release()
        {
            lock (_lock)
                _release?.Set();
        }

        public Task Send(ConsumeContext<LifecycleJob> context)
        {
            Interlocked.Increment(ref _count);
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                ManualResetEventSlim release;
                lock (_lock)
                {
                    _entered!.TrySetResult();
                    release = _release!;
                }

                if (!release.Wait(OperationTimeout()))
                    throw new TimeoutException("The admitted job was not released by the test.");
            }

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }

        public void Dispose()
        {
            lock (_lock)
                _release?.Dispose();
        }
    }

    private sealed class ControlledPublishEndpoint : IPublishEndpoint
    {
        private readonly Dictionary<ConcurrentLimitKind, Queue<PublicationControl>> _controls = [];
        private readonly Dictionary<ConcurrentLimitKind, int> _counts = [];
        private readonly object _lock = new();
        private int _activeHeartbeatPublications;
        private int _maximumConcurrentHeartbeatPublications;

        public int ActiveHeartbeatPublications => Volatile.Read(ref _activeHeartbeatPublications);
        public int MaximumConcurrentHeartbeatPublications => Volatile.Read(ref _maximumConcurrentHeartbeatPublications);

        public PublicationGate BlockNext(ConcurrentLimitKind kind)
        {
            var gate = new PublicationGate();
            Enqueue(kind, new PublicationControl(gate, null));
            return gate;
        }

        public void FailNext(ConcurrentLimitKind kind, Exception exception) =>
            Enqueue(kind, new PublicationControl(null, exception));

        public int Count(ConcurrentLimitKind kind)
        {
            lock (_lock)
                return _counts.GetValueOrDefault(kind);
        }

        public async Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            if (message is not SetConcurrentJobLimit limit)
                return;

            PublicationControl? control;
            lock (_lock)
            {
                _counts[limit.Kind] = _counts.GetValueOrDefault(limit.Kind) + 1;
                control = _controls.TryGetValue(limit.Kind, out Queue<PublicationControl>? queue) && queue.Count > 0
                    ? queue.Dequeue()
                    : null;
            }

            if (control?.Failure is not null)
                throw control.Failure;

            bool heartbeat = limit.Kind == ConcurrentLimitKind.Heartbeat;
            if (heartbeat)
            {
                int active = Interlocked.Increment(ref _activeHeartbeatPublications);
                int observed;
                do
                {
                    observed = Volatile.Read(ref _maximumConcurrentHeartbeatPublications);
                }
                while (active > observed
                    && Interlocked.CompareExchange(ref _maximumConcurrentHeartbeatPublications, active, observed) != observed);
            }

            try
            {
                if (control?.Gate is not null)
                {
                    control.Gate.SignalEntered();
                    await control.Gate.WaitForRelease(cancellationToken);
                }
            }
            finally
            {
                if (heartbeat)
                    Interlocked.Decrement(ref _activeHeartbeatPublications);
            }
        }

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class => Publish(message, cancellationToken);

        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class => Publish(message, cancellationToken);

        public Task Publish(object message, CancellationToken cancellationToken = default) =>
            Publish<object>(message, cancellationToken);

        public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            Publish<object>(message, cancellationToken);

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) =>
            Publish<object>(message, cancellationToken);

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            Publish<object>(message, cancellationToken);

        public Task Publish<T>(object values, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException("The lifecycle service publishes typed messages.");

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException("The lifecycle service publishes typed messages.");

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException("The lifecycle service publishes typed messages.");

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException("The test endpoint records publications directly.");

        private void Enqueue(ConcurrentLimitKind kind, PublicationControl control)
        {
            lock (_lock)
            {
                if (!_controls.TryGetValue(kind, out Queue<PublicationControl>? queue))
                    _controls.Add(kind, queue = new Queue<PublicationControl>());
                queue.Enqueue(control);
            }
        }
    }

    private sealed record PublicationControl(PublicationGate? Gate, Exception? Failure);

    private sealed class PublicationGate
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;
        public void Release() => _release.TrySetResult();
        public void SignalEntered() => _entered.TrySetResult();
        public Task WaitForRelease(CancellationToken cancellationToken) => _release.Task.WaitAsync(cancellationToken);
    }
}
