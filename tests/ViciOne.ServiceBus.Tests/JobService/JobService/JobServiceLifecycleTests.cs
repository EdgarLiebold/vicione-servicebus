using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;
using RuntimeJobService = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobServiceLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-PERSISTENCE", "new-state-has-non-null-persisted-collections")]
    public void NewJobTypeSaga_InitializesEveryPersistedCollection()
    {
        var saga = new JobTypeSaga();

        Assert.Empty(saga.ActiveAllocations);
        Assert.Empty(saga.ServiceInstances);
        Assert.Empty(saga.JobTypeProperties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-PERSISTENCE", "new-job-has-non-null-persisted-dictionaries")]
    public void NewJobSaga_InitializesRequiredPersistedDictionaries()
    {
        var saga = new JobSaga();

        Assert.Empty(saga.Job);
        Assert.Empty(saga.JobProperties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "bidirectional-transition-serialization")]
    public async Task StartAndStopTransitions_AreSerializedInBothDirectionsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var stopFirstEndpoint = new ControlledPublishEndpoint();
        PublicationGate stopped = stopFirstEndpoint.BlockNext(JobConcurrencyUpdateKind.InstanceStopped);
        RuntimeJobService stopFirstService = NewService(TimeSpan.FromDays(1));

        Task stopping = stopFirstService.StopAsync(stopFirstEndpoint, TestContext.Current.CancellationToken);
        await stopped.Entered.WaitAsync(timeout, cancellationToken);
        Task starting = stopFirstService.BusStartedAsync(stopFirstEndpoint, TestContext.Current.CancellationToken);

        Assert.False(starting.IsCompleted);
        Assert.Equal(0, stopFirstEndpoint.Count(JobConcurrencyUpdateKind.Configuration));

        stopped.Release();
        await Task.WhenAll(stopping, starting).WaitAsync(timeout, cancellationToken);
        await stopFirstService.StopAsync(stopFirstEndpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        var startFirstEndpoint = new ControlledPublishEndpoint();
        PublicationGate configured = startFirstEndpoint.BlockNext(JobConcurrencyUpdateKind.Configuration);
        RuntimeJobService startFirstService = NewService(TimeSpan.FromDays(1));

        starting = startFirstService.BusStartedAsync(startFirstEndpoint, TestContext.Current.CancellationToken);
        await configured.Entered.WaitAsync(timeout, cancellationToken);
        stopping = startFirstService.StopAsync(startFirstEndpoint, TestContext.Current.CancellationToken);

        Assert.False(stopping.IsCompleted);
        Assert.Equal(0, startFirstEndpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));

        configured.Release();
        await Task.WhenAll(starting, stopping).WaitAsync(timeout, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "stop-cancels-inflight-heartbeat-and-leaves-no-generation")]
    public async Task Stop_CancelsAndDrainsTheExactHeartbeatGenerationBeforeReturningAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        PublicationGate heartbeat = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        RuntimeJobService service = NewService(TimeSpan.Zero);

        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await heartbeat.Entered.WaitAsync(timeout, cancellationToken);

        Task stopping = service.StopAsync(endpoint, TestContext.Current.CancellationToken);
        try
        {
            await stopping.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            heartbeat.Release();
        }

        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));
        Assert.Equal(0, endpoint.ActiveHeartbeatPublications);
        Assert.Equal(1, endpoint.MaximumConcurrentHeartbeatPublications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "repeated-start-replaces-heartbeat-generation")]
    public async Task RepeatedStarts_CancelAndReplaceHeartbeatGenerationsWithoutOverlapAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        RuntimeJobService service = NewService(TimeSpan.Zero);

        PublicationGate first = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await first.Entered.WaitAsync(timeout, cancellationToken);

        PublicationGate second = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await second.Entered.WaitAsync(timeout, cancellationToken);
        Assert.Equal(1, endpoint.ActiveHeartbeatPublications);

        PublicationGate third = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await third.Entered.WaitAsync(timeout, cancellationToken);
        Assert.Equal(1, endpoint.ActiveHeartbeatPublications);

        await service.StopAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(3, endpoint.Count(JobConcurrencyUpdateKind.Configuration));
        Assert.Equal(3, endpoint.Count(JobConcurrencyUpdateKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));
        Assert.Equal(1, endpoint.MaximumConcurrentHeartbeatPublications);
        Assert.Equal(0, endpoint.ActiveHeartbeatPublications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "heartbeat-publication-failure-does-not-stop-the-generation")]
    public async Task HeartbeatPublicationFailure_DoesNotStopTheActiveGenerationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        endpoint.FailNext(JobConcurrencyUpdateKind.Heartbeat, new InvalidOperationException("heartbeat refused"));
        PublicationGate recoveredHeartbeat = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        RuntimeJobService service = NewService(TimeSpan.Zero);

        await service.BusStartedAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);
        await recoveredHeartbeat.Entered.WaitAsync(timeout, cancellationToken);
        await service.StopAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(2, endpoint.Count(JobConcurrencyUpdateKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));
        Assert.Equal(0, endpoint.ActiveHeartbeatPublications);
        Assert.Equal(1, endpoint.MaximumConcurrentHeartbeatPublications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "failed-start-has-no-heartbeat-and-recovers")]
    public async Task FailedStart_LeavesNoHeartbeatAndDoesNotStrandTheLifecycleGateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var endpoint = new ControlledPublishEndpoint();
        RuntimeJobService service = NewService(TimeSpan.Zero);
        var expected = new InvalidOperationException("configured announcement refused");
        endpoint.FailNext(JobConcurrencyUpdateKind.Configuration, expected);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(0, endpoint.Count(JobConcurrencyUpdateKind.Heartbeat));

        PublicationGate heartbeat = endpoint.BlockNext(JobConcurrencyUpdateKind.Heartbeat);
        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await heartbeat.Entered.WaitAsync(timeout, cancellationToken);
        Task stopping = service.StopAsync(endpoint, TestContext.Current.CancellationToken);
        heartbeat.Release();
        await stopping.WaitAsync(timeout, cancellationToken);

        Assert.Equal(2, endpoint.Count(JobConcurrencyUpdateKind.Configuration));
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.Heartbeat));
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-CONFIGURATION", "registered-job-type-uses-an-isolated-snapshot")]
    public async Task RegisteredJobType_SnapshotsConcurrencyAndMetadataBeforeRuntimeStartsAsync()
    {
        var options = new JobOptions<LifecycleJob>
        {
            ConcurrentJobLimit = 2,
            GlobalConcurrentJobLimit = 5,
        };
        options.JobTypeProperties.Set("tier", "gold");
        options.InstanceProperties.Set("region", "west");
        var service = new RuntimeJobService(new StubJobServiceSettings(TimeSpan.FromDays(1)));
        service.RegisterJobType(options, NewId.NextGuid(), "lifecycle-job");

        options.ConcurrentJobLimit = 7;
        options.GlobalConcurrentJobLimit = 11;
        options.JobTypeProperties.Set("tier", "silver");
        options.InstanceProperties.Set("region", "east");

        var endpoint = new ControlledPublishEndpoint();
        await service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken);
        ISetConcurrentJobLimit announcement = endpoint.Single(JobConcurrencyUpdateKind.Configuration);
        await service.StopAsync(endpoint, TestContext.Current.CancellationToken);

        Assert.Equal(2, announcement.ConcurrentJobLimit);
        Assert.Equal(5, announcement.GlobalConcurrentJobLimit);
        Assert.Equal("gold", announcement.JobTypeProperties?["tier"]);
        Assert.Equal("west", announcement.InstanceProperties?["region"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-CONFIGURATION", "registration-enforces-identity-and-single-ownership")]
    public void JobTypeRegistration_EnforcesRequiredInputsIdentityAndSingleOwnership()
    {
        var service = new RuntimeJobService(new StubJobServiceSettings(TimeSpan.FromDays(1)));
        var options = new JobOptions<LifecycleJob>();
        Guid jobTypeId = NewId.NextGuid();

        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            service.RegisterJobType<LifecycleJob>(null!, jobTypeId, "lifecycle-job")).ParamName);
        Assert.Equal("jobTypeId", Assert.Throws<ArgumentException>(() =>
            service.RegisterJobType(options, Guid.Empty, "lifecycle-job")).ParamName);
        Assert.Equal("jobTypeName", Assert.Throws<ArgumentException>(() =>
            service.RegisterJobType(options, jobTypeId, " ")).ParamName);

        service.RegisterJobType(options, jobTypeId, "lifecycle-job");

        Assert.Equal(jobTypeId, service.GetJobTypeId<LifecycleJob>());
        Assert.Throws<ConfigurationException>(() =>
            service.RegisterJobType(new JobOptions<LifecycleJob>(), NewId.NextGuid(), "second-registration"));
        Assert.Throws<ConfigurationException>(() => service.GetJobTypeId<UnregisteredLifecycleJob>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "complete-start-stop-failure-admission-contract")]
    public async Task Admission_FollowsTheCompleteSuccessfulAndFailedLifecycleSequenceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AdmissionFixture fixture = await AdmissionFixture.StartAsync(timeout, cancellationToken);
        var endpoint = new ControlledPublishEndpoint();

        await fixture.Service.StopAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var failedStart = new InvalidOperationException("first start refused");
        endpoint.FailNext(JobConcurrencyUpdateKind.Configuration, failedStart);
        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken));
        Assert.Same(failedStart, actual);
        Assert.False(await fixture.SubmitAsync(cancellationToken));

        await fixture.Service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.SubmitAsync(cancellationToken));

        PublicationGate stopped = endpoint.BlockNext(JobConcurrencyUpdateKind.InstanceStopped);
        Task stopping = fixture.Service.StopAsync(endpoint, TestContext.Current.CancellationToken);
        await stopped.Entered.WaitAsync(timeout, cancellationToken);
        Assert.False(await fixture.SubmitAsync(cancellationToken));
        stopped.Release();
        await stopping.WaitAsync(timeout, cancellationToken);
        Assert.False(await fixture.SubmitAsync(cancellationToken));

        await fixture.Service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.SubmitAsync(cancellationToken));
        var failedRestart = new InvalidOperationException("restart refused");
        endpoint.FailNext(JobConcurrencyUpdateKind.Configuration, failedRestart);
        actual = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken));
        Assert.Same(failedRestart, actual);
        Assert.False(await fixture.SubmitAsync(cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "stop-waits-for-admitted-job-registration")]
    public async Task Stop_WaitsForAnAdmittedJobUntilItsHandleIsRegisteredAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AdmissionFixture fixture = await AdmissionFixture.StartAsync(timeout, cancellationToken);
        var endpoint = new ControlledPublishEndpoint();
        await fixture.Service.StopAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        await fixture.Service.BusStartedAsync(endpoint, TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Assert.True(await fixture.SubmitAsync(cancellationToken));

        fixture.JobPipe.HoldNext();
        Task<bool> submitting = fixture.SubmitAsync(cancellationToken);
        await fixture.JobPipe.Entered.WaitAsync(timeout, cancellationToken);

        Task stopping = fixture.Service.StopAsync(endpoint, TestContext.Current.CancellationToken);

        Assert.False(stopping.IsCompleted);

        fixture.JobPipe.Release();
        Assert.True(await submitting.WaitAsync(timeout, cancellationToken));
        await stopping.WaitAsync(timeout, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-LIFECYCLE", "stop-cancels-and-removes-an-active-local-execution")]
    public async Task Stop_CancelsAndRemovesAnActiveLocalExecutionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeJobService service = NewService(TimeSpan.FromDays(1));
        var endpoint = new ControlledPublishEndpoint();
        await service.BusStartedAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);

        var command = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            JobTypeId = service.GetJobTypeId<LifecycleJob>(),
            Job = new Dictionary<string, object>(),
        };
        ConsumeContext<IStartJob> context = InMemoryOutboxTestContextFactory.Create<IStartJob>(
            command,
            cancellationToken);
        var pipe = new CancellationObservingJobPipe();

        await service.StartJobAsync(
            context,
            new LifecycleJob(),
            pipe,
            new JobOptions<LifecycleJob> { JobCancellationTimeout = timeout },
            cancellationToken);
        Assert.True(service.TryGetJob(command.JobId, out IJobHandle? active));
        Assert.NotNull(active);

        await service.StopAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);

        await pipe.CancellationObserved.WaitAsync(timeout, cancellationToken);
        Assert.False(service.TryGetJob(command.JobId, out IJobHandle? removed));
        Assert.Null(removed);
        Assert.Equal(1, endpoint.Count(JobConcurrencyUpdateKind.InstanceStopped));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "active-job-identities-are-unique")]
    public async Task ActiveJobIdentity_CannotBeAdmittedTwiceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeJobService service = NewService(TimeSpan.FromDays(1));
        var endpoint = new ControlledPublishEndpoint();
        await service.BusStartedAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);

        var command = CreateStartCommand(service);
        var pipe = new CancellationObservingJobPipe();
        await service.StartJobAsync(
            CreateStartContext(command, cancellationToken),
            new LifecycleJob(),
            pipe,
            new JobOptions<LifecycleJob> { JobCancellationTimeout = timeout },
            cancellationToken);

        JobAlreadyExistsException exception = await Assert.ThrowsAsync<JobAlreadyExistsException>(() => service.StartJobAsync(
            CreateStartContext(command, cancellationToken),
            new LifecycleJob(),
            new CountingJobPipe(),
            new JobOptions<LifecycleJob>(),
            cancellationToken));

        Assert.Equal(command.JobId, exception.JobId);
        Assert.True(service.TryGetJob(command.JobId, out _));

        await service.StopAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);
        await pipe.CancellationObserved.WaitAsync(timeout, cancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-ADMISSION", "synchronous-pipeline-failure-releases-reservation")]
    public async Task SynchronousPipelineFailure_ReleasesTheReservedIdentityAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeJobService service = NewService(TimeSpan.FromDays(1));
        var endpoint = new ControlledPublishEndpoint();
        await service.BusStartedAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);

        var command = CreateStartCommand(service);
        var expected = new InvalidOperationException("consumer pipe refused the job");
        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartJobAsync(
            CreateStartContext(command, cancellationToken),
            new LifecycleJob(),
            new SynchronouslyThrowingJobPipe(expected),
            new JobOptions<LifecycleJob>(),
            cancellationToken));

        Assert.Same(expected, actual);
        Assert.False(service.TryGetJob(command.JobId, out _));

        var replacement = new CancellationObservingJobPipe();
        await service.StartJobAsync(
            CreateStartContext(command, cancellationToken),
            new LifecycleJob(),
            replacement,
            new JobOptions<LifecycleJob> { JobCancellationTimeout = timeout },
            cancellationToken);
        Assert.True(service.TryGetJob(command.JobId, out _));

        await service.StopAsync(endpoint, cancellationToken).WaitAsync(timeout, cancellationToken);
        await replacement.CancellationObserved.WaitAsync(timeout, cancellationToken);
    }

    private static StartJobCommand CreateStartCommand(RuntimeJobService service) => new()
    {
        JobId = NewId.NextGuid(),
        AttemptId = NewId.NextGuid(),
        JobTypeId = service.GetJobTypeId<LifecycleJob>(),
        Job = new Dictionary<string, object>(),
    };

    private static ConsumeContext<IStartJob> CreateStartContext(StartJobCommand command, CancellationToken cancellationToken) =>
        InMemoryOutboxTestContextFactory.Create<IStartJob>(command, cancellationToken);

    private static RuntimeJobService NewService(TimeSpan heartbeatInterval)
    {
        var service = new RuntimeJobService(new StubJobServiceSettings(heartbeatInterval));
        service.RegisterJobType(new JobOptions<LifecycleJob>(), NewId.NextGuid(), "lifecycle-job");
        return service;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class StubJobServiceSettings(TimeSpan heartbeatInterval) : IJobServiceSettings
    {
        public IJobService Runtime => throw new NotSupportedException("The lifecycle tests drive the service directly.");
        public TimeSpan HeartbeatInterval { get; } = heartbeatInterval;
        public TimeSpan RejectedJobDelay { get; } = TimeSpan.Zero;
        public TimeProvider TimeProvider => System.TimeProvider.System;
        public Uri InstanceAddress { get; } = new("loopback://localhost/job-instance");
        public IReceiveEndpointConfigurator InstanceEndpoint => null!;

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }

    private sealed record LifecycleJob;

    private sealed record UnregisteredLifecycleJob;

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

        public static async Task<AdmissionFixture> StartAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            RuntimeJobService service = NewService(TimeSpan.FromDays(1));
            var jobPipe = new CountingJobPipe();
            var services = new ServiceCollection();
            services.AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<IStartJob>(async context =>
                {
                    TaskCompletionSource? handled = jobPipe.Handled;
                    try
                    {
                        await service.StartJobAsync(context, new LifecycleJob(), jobPipe, new JobOptions<LifecycleJob>(), cancellationToken: cancellationToken);
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
                ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken).WaitAsync(timeout, cancellationToken);
                return new AdmissionFixture(provider, harness, service, jobPipe, timeout);
            }
            catch
            {
                await provider.DisposeAsync();
                jobPipe.Dispose();
                throw;
            }
        }

        public async Task<bool> SubmitAsync(CancellationToken cancellationToken)
        {
            int before = JobPipe.Count;
            var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobPipe.Handled = handled;
            ISendEndpoint handlerEndpoint = await _harness.GetHandlerEndpointAsync<IStartJob>(cancellationToken: cancellationToken);
            await handlerEndpoint.SendAsync<IStartJob>(new
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
                await _harness.StopAsync(CancellationToken.None).WaitAsync(_timeout, CancellationToken.None);
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

        public Task SendAsync(ConsumeContext<LifecycleJob> context)
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

    private sealed class CancellationObservingJobPipe : IPipe<ConsumeContext<LifecycleJob>>
    {
        private readonly TaskCompletionSource _cancellationObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CancellationObserved => _cancellationObserved.Task;

        public void Probe(ProbeContext context)
        {
        }

        public async Task SendAsync(ConsumeContext<LifecycleJob> context)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == context.CancellationToken)
            {
                _cancellationObserved.TrySetResult();
                throw;
            }
        }
    }

    private sealed class SynchronouslyThrowingJobPipe(Exception exception) : IPipe<ConsumeContext<LifecycleJob>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<LifecycleJob> context) => throw exception;
    }

    private sealed class ControlledPublishEndpoint : IPublishEndpoint
    {
        private readonly Dictionary<JobConcurrencyUpdateKind, Queue<PublicationControl>> _controls = [];
        private readonly Dictionary<JobConcurrencyUpdateKind, int> _counts = [];
        private readonly List<ISetConcurrentJobLimit> _messages = [];
        private readonly object _lock = new();
        private int _activeHeartbeatPublications;
        private int _maximumConcurrentHeartbeatPublications;

        public int ActiveHeartbeatPublications => Volatile.Read(ref _activeHeartbeatPublications);
        public int MaximumConcurrentHeartbeatPublications => Volatile.Read(ref _maximumConcurrentHeartbeatPublications);

        public PublicationGate BlockNext(JobConcurrencyUpdateKind updateKind)
        {
            var gate = new PublicationGate();
            Enqueue(updateKind, new PublicationControl(gate, null));
            return gate;
        }

        public void FailNext(JobConcurrencyUpdateKind updateKind, Exception exception) =>
            Enqueue(updateKind, new PublicationControl(null, exception));

        public int Count(JobConcurrencyUpdateKind updateKind)
        {
            lock (_lock)
                return _counts.GetValueOrDefault(updateKind);
        }

        public ISetConcurrentJobLimit Single(JobConcurrencyUpdateKind updateKind)
        {
            lock (_lock)
                return Assert.Single(_messages, message => message.UpdateKind == updateKind);
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            if (message is not ISetConcurrentJobLimit limit)
                return;

            PublicationControl? control;
            lock (_lock)
            {
                _counts[limit.UpdateKind] = _counts.GetValueOrDefault(limit.UpdateKind) + 1;
                _messages.Add(limit);
                control = _controls.TryGetValue(limit.UpdateKind, out Queue<PublicationControl>? queue) && queue.Count > 0
                    ? queue.Dequeue()
                    : null;
            }

            if (control?.Failure is not null)
                throw control.Failure;

            bool heartbeat = limit.UpdateKind == JobConcurrencyUpdateKind.Heartbeat;
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
                    await control.Gate.WaitForReleaseAsync(cancellationToken);
                }
            }
            finally
            {
                if (heartbeat)
                    Interlocked.Decrement(ref _activeHeartbeatPublications);
            }
        }

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return PublishAsync(message, cancellationToken);
        }

        public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class => PublishAsync(message, cancellationToken);

        public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class => PublishAsync(message, cancellationToken);

        public Task PublishAsync(object message, CancellationToken cancellationToken = default) =>
            PublishAsync<object>(message, cancellationToken);

        public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            PublishAsync<object>(message, cancellationToken);

        public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default) =>
            PublishAsync<object>(message, cancellationToken);

        public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
            PublishAsync<object>(message, cancellationToken);

        public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
            where T : class
        { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException("The lifecycle service publishes typed messages."); }
        public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException("The lifecycle service publishes typed messages."); }
        public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException("The lifecycle service publishes typed messages."); }
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException("The test endpoint records publications directly.");

        private void Enqueue(JobConcurrencyUpdateKind updateKind, PublicationControl control)
        {
            lock (_lock)
            {
                if (!_controls.TryGetValue(updateKind, out Queue<PublicationControl>? queue))
                    _controls.Add(updateKind, queue = new Queue<PublicationControl>());
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
        public Task WaitForReleaseAsync(CancellationToken cancellationToken) => _release.Task.WaitAsync(cancellationToken);
    }
}
