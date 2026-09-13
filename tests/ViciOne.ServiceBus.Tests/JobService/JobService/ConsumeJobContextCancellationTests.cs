using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using RuntimeJobService = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class ConsumeJobContextCancellationTests
{
    [Theory]
    [InlineData(JobOperation.NotifyCanceled)]
    [InlineData(JobOperation.NotifyStarted)]
    [InlineData(JobOperation.NotifyCompleted)]
    [InlineData(JobOperation.NotifyProgress)]
    [InlineData(JobOperation.NotifyFaulted)]
    [InlineData(JobOperation.SaveCheckpoint)]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "every-notification-forwards-the-operation-token")]
    public async Task NotificationOperation_ForwardsTheExactCancellationTokenAsync(JobOperation operation)
    {
        var endpoint = new RecordingSendEndpoint();
        var provider = new RecordingPublishEndpointProvider(endpoint);
        ConsumeContext<StartJob> consumeContext = CreateContext(provider);
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await (operation switch
        {
            JobOperation.NotifyCanceled => context.NotifyCanceledAsync(source.Token),
            JobOperation.NotifyStarted => context.NotifyStartedAsync(source.Token),
            JobOperation.NotifyCompleted => context.NotifyCompletedAsync(source.Token),
            JobOperation.NotifyProgress => context.NotifyProgressAsync(
                new SetJobProgressCommand
                {
                    JobId = context.JobId,
                    AttemptId = context.AttemptId,
                    SequenceNumber = 1,
                    Value = 42,
                    Limit = 100,
                },
                source.Token),
            JobOperation.NotifyFaulted => context.NotifyFaultedAsync(new InvalidOperationException("expected"), null, source.Token),
            JobOperation.SaveCheckpoint => context.SaveCheckpointAsync<object>(null, source.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        });

        Assert.NotEmpty(provider.CancellationTokens);
        Assert.All(provider.CancellationTokens, token => Assert.Equal(source.Token, token));
        Assert.NotEmpty(endpoint.CancellationTokens);
        Assert.All(endpoint.CancellationTokens, token => Assert.Equal(source.Token, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "progress-enqueue-observes-the-operation-token")]
    public async Task ReportProgress_ObservesAnAlreadyCanceledOperationTokenAsync()
    {
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        ConsumeContext<StartJob> consumeContext = CreateContext(provider);
        var options = new JobOptions<TestJob>();
        options.ProgressBuffer.UpdateLimit = 1;
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            options);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.ReportProgressAsync(42, 100, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "job-handle-wait-observes-caller-token")]
    public async Task CancelJobHandle_ObservesCallerCancellationWhileWaitingForTheJobAsync()
    {
        var clock = new FakeTimeProvider();
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        ConsumeContext<StartJob> consumeContext = CreateContext(provider, clock);
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());
        var job = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new ConsumerJobHandle<TestJob>(context, job.Task, TimeSpan.FromHours(1));
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task cancellation = handle.CancelAsync("caller requested", source.Token);
        Assert.False(cancellation.IsCompleted);
        source.Cancel();
        clock.Advance(TimeSpan.FromHours(1));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancellation);
        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(context.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "job-handle-releases-ownership-at-cancellation-deadline")]
    public async Task CancelJobHandle_ReleasesLocalOwnershipWhenExecutionIgnoresCancellationAsync()
    {
        var clock = new FakeTimeProvider();
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        ConsumeContext<StartJob> consumeContext = CreateContext(provider, clock);
        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new ConsumerJobHandle<TestJob>(context, execution.Task, TimeSpan.FromMinutes(1));

        Task cancellation = handle.CancelAsync("shutdown", TestContext.Current.CancellationToken);
        Assert.False(cancellation.IsCompleted);

        clock.Advance(TimeSpan.FromMinutes(1));

        await cancellation;
        await handle.Completion;
        Assert.False(execution.Task.IsCompleted);
        Assert.True(context.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "admission-snapshots-cancellation-deadline-before-consumer-execution")]
    public async Task StartJobAsync_SnapshotsTheCancellationDeadlineBeforeInvokingTheConsumerPipeAsync()
    {
        var clock = new FakeTimeProvider();
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        var message = new StartJobMessage();
        ConsumeContext<StartJob> consumeContext = CreateContext(provider, clock, message);
        var options = new JobOptions<TestJob>
        {
            JobCancellationTimeout = TimeSpan.FromMinutes(1),
            JobTimeout = TimeSpan.FromDays(2),
        };
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IPipe<ConsumeContext<TestJob>> pipe = Pipe.ExecuteAwaited<ConsumeContext<TestJob>>(_ =>
        {
            options.JobCancellationTimeout = TimeSpan.FromDays(1);
            return execution.Task;
        });
        var service = new RuntimeJobService(new StubJobServiceSettings(clock));

        await service.StartJobAsync(
            consumeContext,
            new TestJob(),
            pipe,
            options,
            TestContext.Current.CancellationToken);
        Assert.True(service.TryGetJob(message.JobId, out JobHandle? handle));
        Assert.NotNull(handle);

        Task cancellation = handle.CancelAsync("shutdown", TestContext.Current.CancellationToken);
        Assert.False(cancellation.IsCompleted);
        clock.Advance(TimeSpan.FromMinutes(1));

        await cancellation;
        await handle.Completion;
        execution.TrySetResult();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "missing-job-properties-produce-empty-collection")]
    public async Task Constructor_AcceptsAStartCommandWithoutJobPropertiesAsync()
    {
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        ConsumeContext<StartJob> consumeContext = CreateContext(
            provider,
            message: new StartJobMessage { JobProperties = null });

        await using var context = new ConsumeJobContext<TestJob>(
            consumeContext,
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());

        Assert.Empty(context.JobProperties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "execution-context-exposes-isolated-read-only-snapshots")]
    public async Task Constructor_SnapshotsAllMetadataWithoutExposingMutationCapabilitiesAsync()
    {
        var options = new JobOptions<TestJob>();
        options.JobTypeProperties.Set("tier", "gold");
        options.InstanceProperties.Set("region", "west");
        var messageProperties = new Dictionary<string, object> { ["tenant"] = "one" };
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        await using var context = new ConsumeJobContext<TestJob>(
            CreateContext(provider, message: new StartJobMessage { JobProperties = messageProperties }),
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            options);

        options.JobTypeProperties.Set("tier", "silver");
        options.InstanceProperties.Set("region", "east");
        messageProperties["tenant"] = "two";

        Assert.Equal("gold", context.JobTypeProperties.Get<string>("tier"));
        Assert.Equal("west", context.InstanceProperties.Get<string>("region"));
        Assert.Equal("one", context.JobProperties.Get<string>("tenant"));
        Assert.IsNotAssignableFrom<ISetPropertyCollection>(context.JobTypeProperties);
        Assert.IsNotAssignableFrom<ISetPropertyCollection>(context.InstanceProperties);
        Assert.IsNotAssignableFrom<ISetPropertyCollection>(context.JobProperties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS", "concurrent-first-reports-share-one-ordered-buffer")]
    public async Task ConcurrentFirstProgressReports_UseOneOrderedBufferAsync()
    {
        const int UpdateCount = 64;
        var endpoint = new RecordingSendEndpoint();
        var options = new JobOptions<TestJob>();
        options.ProgressBuffer.UpdateLimit = 1;
        options.ProgressBuffer.TimeLimit = TimeSpan.FromDays(1);
        var context = new ConsumeJobContext<TestJob>(
            CreateContext(new RecordingPublishEndpointProvider(endpoint)),
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            options);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            Task[] reports = Enumerable.Range(1, UpdateCount)
                .Select(async value =>
                {
                    await start.Task;
                    await context.ReportProgressAsync(value, UpdateCount, TestContext.Current.CancellationToken);
                })
                .ToArray();

            start.SetResult();
            await Task.WhenAll(reports);
        }
        finally
        {
            await context.DisposeAsync();
        }

        SetJobProgress[] progress = endpoint.Messages.OfType<SetJobProgress>().ToArray();
        Assert.Equal(UpdateCount, progress.Length);
        Assert.Equal(
            Enumerable.Range(1, UpdateCount).Select(static value => (long)value),
            progress.Select(static update => update.SequenceNumber));
        Assert.Equal(
            Enumerable.Range(1, UpdateCount).Select(static value => (long)value).Order(),
            progress.Select(static update => update.Value).Order());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-NOTIFICATIONS", "typed-start-event-includes-job-payload")]
    public async Task NotifyStartedAsync_PublishesTheTypedJobPayloadAsync()
    {
        var endpoint = new RecordingSendEndpoint();
        var provider = new RecordingPublishEndpointProvider(endpoint);
        var job = new TestJob();
        await using var context = new ConsumeJobContext<TestJob>(
            CreateContext(provider),
            new Uri("loopback://localhost/job-instance"),
            job,
            new JobOptions<TestJob>());

        await context.NotifyStartedAsync(TestContext.Current.CancellationToken);

        JobStarted<TestJob> started = Assert.IsAssignableFrom<JobStarted<TestJob>>(endpoint.Messages[^1]);
        Assert.Same(job, started.Job);
        Assert.Equal(context.JobId, started.JobId);
        Assert.Equal(context.AttemptId, started.AttemptId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-NOTIFICATIONS", "required-notification-values-are-rejected")]
    public async Task NotificationMethods_RejectMissingRequiredValuesAsync()
    {
        var provider = new RecordingPublishEndpointProvider(new RecordingSendEndpoint());
        await using var context = new ConsumeJobContext<TestJob>(
            CreateContext(provider),
            new Uri("loopback://localhost/job-instance"),
            new TestJob(),
            new JobOptions<TestJob>());

        Assert.Equal(
            "progress",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                context.NotifyProgressAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "exception",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                context.NotifyFaultedAsync(null!, null, TestContext.Current.CancellationToken))).ParamName);
    }

    private static ConsumeContext<StartJob> CreateContext(
        IPublishEndpointProvider publishEndpointProvider,
        TimeProvider? timeProvider = null,
        StartJob? message = null)
    {
        TestConsumeContext<StartJob> context = DispatchProxy.Create<TestConsumeContext<StartJob>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(message ?? new StartJobMessage(), publishEndpointProvider, timeProvider);
        return context;
    }

    private interface TestConsumeContext<out T> : ConsumeContext<T>, ConsumeContext
        where T : class;

    private class ConsumeContextProxy : DispatchProxy
    {
        private object _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;
        private TimeProvider? _timeProvider;

        public void Configure(object message, IPublishEndpointProvider publishEndpointProvider, TimeProvider? timeProvider)
        {
            _message = message;
            _timeProvider = timeProvider;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).PublishEndpointProvider = publishEndpointProvider;
            _serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_Message":
                    return _message;
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "get_CancellationToken":
                    return TestContext.Current.CancellationToken;
                case "HasPayloadType":
                    return false;
                case "TryGetPayload":
                    if (targetMethod.GetGenericArguments()[0] == typeof(TimeProvider) && _timeProvider is not null)
                    {
                        args![0] = _timeProvider;
                        return true;
                    }

                    args![0] = null;
                    return false;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class RecordingPublishEndpointProvider(RecordingSendEndpoint endpoint) : IPublishEndpointProvider
    {
        public List<CancellationToken> CancellationTokens { get; } = [];

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            CancellationTokens.Add(cancellationToken);
            return Task.FromResult<ISendEndpoint>(endpoint);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSendEndpoint : ISendEndpoint
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
        public List<object> Messages { get; } = [];

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            CancellationTokens.Add(cancellationToken);
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(options);
            return SendAsync(message, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class StartJobMessage : StartJob
    {
        public Guid JobId { get; } = Guid.NewGuid();
        public Guid AttemptId { get; } = Guid.NewGuid();
        public int RetryAttempt => 0;
        public IReadOnlyDictionary<string, object> Job { get; } = new Dictionary<string, object>();
        public Guid JobTypeId { get; } = Guid.NewGuid();
        public long? LastProgressValue => null;
        public long? LastProgressLimit => null;
        public IReadOnlyDictionary<string, object>? Checkpoint => null;
        public IReadOnlyDictionary<string, object>? JobProperties { get; init; } = new Dictionary<string, object>();
    }

    private sealed record TestJob;

    private sealed class StubJobServiceSettings(TimeProvider timeProvider) : JobServiceSettings
    {
        public IJobService Runtime => throw new NotSupportedException("The test drives the job service directly.");
        public TimeSpan HeartbeatInterval => TimeSpan.FromDays(1);
        public TimeSpan RejectedJobDelay => TimeSpan.FromSeconds(1);
        public TimeProvider TimeProvider { get; } = timeProvider;
        public Uri InstanceAddress { get; } = new("loopback://localhost/job-instance");
        public IReceiveEndpointConfigurator InstanceEndpoint => throw new NotSupportedException();

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }

    public enum JobOperation
    {
        NotifyCanceled,
        NotifyStarted,
        NotifyCompleted,
        NotifyProgress,
        NotifyFaulted,
        SaveCheckpoint,
    }
}
