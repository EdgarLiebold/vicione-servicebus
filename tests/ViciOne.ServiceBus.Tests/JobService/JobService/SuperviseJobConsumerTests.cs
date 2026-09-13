using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class SuperviseJobConsumerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUPERVISION", "stale-attempt-cannot-cancel-current-execution")]
    public async Task CancelJobAttempt_IgnoresARequestForAnEarlierAttemptAsync()
    {
        Guid jobId = NewId.NextGuid();
        var handle = new RecordingJobHandle(jobId, NewId.NextGuid());
        var consumer = new SuperviseJobConsumer(new StubJobService(handle));
        ConsumeContext<CancelJobAttempt> context = CreateContext(
            new CancelJobAttemptMessage(jobId, NewId.NextGuid(), "stale attempt"));

        await consumer.ConsumeAsync(context);

        Assert.Equal(0, handle.CancellationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUPERVISION", "matching-attempt-receives-cancellation-reason-and-token")]
    public async Task CancelJobAttempt_CancelsOnlyTheMatchingAttemptAsync()
    {
        Guid jobId = NewId.NextGuid();
        Guid attemptId = NewId.NextGuid();
        var handle = new RecordingJobHandle(jobId, attemptId);
        var consumer = new SuperviseJobConsumer(new StubJobService(handle));
        ConsumeContext<CancelJobAttempt> context = CreateContext(
            new CancelJobAttemptMessage(jobId, attemptId, "operator request"));

        await consumer.ConsumeAsync(context);

        Assert.Equal(1, handle.CancellationCount);
        Assert.Equal("operator request", handle.CancellationReason);
        Assert.Equal(TestContext.Current.CancellationToken, handle.CancellationToken);
    }

    [Theory]
    [InlineData(TaskStatus.WaitingForActivation, JobAttemptStatusKind.Running)]
    [InlineData(TaskStatus.RanToCompletion, JobAttemptStatusKind.Completed)]
    [InlineData(TaskStatus.Faulted, JobAttemptStatusKind.Faulted)]
    [InlineData(TaskStatus.Canceled, JobAttemptStatusKind.Canceled)]
    [RequirementCoverage("REQ-VSB-JOB-SUPERVISION", "every-local-task-state-has-an-explicit-wire-status")]
    public async Task GetJobAttemptStatus_MapsEveryLocalTaskStateAsync(
        TaskStatus taskStatus,
        JobAttemptStatusKind expectedStatus)
    {
        Guid jobId = NewId.NextGuid();
        Guid attemptId = NewId.NextGuid();
        Task execution = CreateTaskAsync(taskStatus);
        var handle = new RecordingJobHandle(jobId, attemptId, execution);
        var consumer = new SuperviseJobConsumer(new StubJobService(handle));
        var clock = new FakeTimeProvider(new DateTimeOffset(2047, 4, 5, 6, 7, 8, TimeSpan.Zero));
        TestStatusConsumeContext context = CreateStatusContext(
            new GetJobAttemptStatusMessage(jobId, attemptId),
            clock);

        await consumer.ConsumeAsync(context);

        JobAttemptStatus response = Assert.IsAssignableFrom<JobAttemptStatus>(
            ((StatusConsumeContextProxy)(object)context).Response);
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(attemptId, response.AttemptId);
        Assert.Equal(clock.GetUtcNow(), response.Timestamp);
        Assert.Equal(expectedStatus, response.Status);
        _ = execution.Exception;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUPERVISION", "missing-or-stale-attempt-produces-no-status")]
    public async Task GetJobAttemptStatus_DoesNotAnswerForMissingOrStaleAttemptsAsync()
    {
        Guid jobId = NewId.NextGuid();
        var handle = new RecordingJobHandle(jobId, NewId.NextGuid());
        var consumer = new SuperviseJobConsumer(new StubJobService(handle));
        var clock = new FakeTimeProvider();
        TestStatusConsumeContext stale = CreateStatusContext(
            new GetJobAttemptStatusMessage(jobId, NewId.NextGuid()),
            clock);
        TestStatusConsumeContext missing = CreateStatusContext(
            new GetJobAttemptStatusMessage(NewId.NextGuid(), NewId.NextGuid()),
            clock);

        await consumer.ConsumeAsync(stale);
        await consumer.ConsumeAsync(missing);

        Assert.Null(((StatusConsumeContextProxy)(object)stale).Response);
        Assert.Null(((StatusConsumeContextProxy)(object)missing).Response);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUPERVISION", "required-consumer-inputs-are-validated")]
    public async Task Supervisor_RejectsMissingRuntimeAndConsumeContextsAsync()
    {
        Assert.Equal("jobService", Assert.Throws<ArgumentNullException>(() => new SuperviseJobConsumer(null!)).ParamName);
        var consumer = new SuperviseJobConsumer(new StubJobService(new RecordingJobHandle(NewId.NextGuid(), NewId.NextGuid())));

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            consumer.ConsumeAsync((ConsumeContext<CancelJobAttempt>)null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            consumer.ConsumeAsync((ConsumeContext<GetJobAttemptStatus>)null!))).ParamName);
    }

    private static ConsumeContext<CancelJobAttempt> CreateContext(CancelJobAttempt message)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Message = message;
        return context;
    }

    private static TestStatusConsumeContext CreateStatusContext(GetJobAttemptStatus message, TimeProvider timeProvider)
    {
        TestStatusConsumeContext context = DispatchProxy.Create<TestStatusConsumeContext, StatusConsumeContextProxy>();
        ((StatusConsumeContextProxy)(object)context).Configure(message, timeProvider);
        return context;
    }

    private static Task CreateTaskAsync(TaskStatus status) => status switch
    {
        TaskStatus.WaitingForActivation => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task,
        TaskStatus.RanToCompletion => Task.CompletedTask,
        TaskStatus.Faulted => Task.FromException(new InvalidOperationException("expected fault")),
        TaskStatus.Canceled => Task.FromCanceled(new CancellationToken(canceled: true)),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private interface TestConsumeContext : ConsumeContext<CancelJobAttempt>, ConsumeContext;

    private interface TestStatusConsumeContext : ConsumeContext<GetJobAttemptStatus>, ConsumeContext;

    private class ConsumeContextProxy : DispatchProxy
    {
        public CancelJobAttempt Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_Message" => Message,
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private class StatusConsumeContextProxy : DispatchProxy
    {
        private GetJobAttemptStatus _message = null!;
        private TimeProvider _timeProvider = null!;

        public object? Response { get; private set; }

        public void Configure(GetJobAttemptStatus message, TimeProvider timeProvider)
        {
            _message = message;
            _timeProvider = timeProvider;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_Message")
                return _message;
            if (targetMethod.Name == "get_CancellationToken")
                return TestContext.Current.CancellationToken;
            if (targetMethod.Name == nameof(PipeContext.TryGetPayload))
            {
                bool isTimeProvider = targetMethod.GetGenericArguments()[0] == typeof(TimeProvider);
                args![0] = isTimeProvider ? _timeProvider : null;
                return isTimeProvider;
            }
            if (targetMethod.Name == nameof(ConsumeContext.RespondAsync))
            {
                Response = args![0];
                return Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed record CancelJobAttemptMessage(Guid JobId, Guid AttemptId, string? Reason) : CancelJobAttempt;

    private sealed record GetJobAttemptStatusMessage(Guid JobId, Guid AttemptId) : GetJobAttemptStatus;

    private sealed class RecordingJobHandle(Guid jobId, Guid attemptId, Task? execution = null) : JobHandle
    {
        public Guid JobId { get; } = jobId;
        public Guid AttemptId { get; } = attemptId;
        public Task Execution { get; } = execution ?? Task.CompletedTask;
        public Task Completion => Task.CompletedTask;
        public int CancellationCount { get; private set; }
        public string? CancellationReason { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task CancelAsync(string? reason, CancellationToken cancellationToken = default)
        {
            CancellationCount++;
            CancellationReason = reason;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StubJobService(JobHandle handle) : IJobService
    {
        public Uri InstanceAddress => throw new NotSupportedException();
        public JobServiceSettings Settings => throw new NotSupportedException();

        public Task StartJobAsync<TJob>(
            ConsumeContext<StartJob> context,
            TJob job,
            IPipe<ConsumeContext<TJob>> jobPipe,
            JobOptions<TJob> jobOptions,
            CancellationToken cancellationToken = default)
            where TJob : class => throw new NotSupportedException();

        public Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference)
        {
            jobReference = jobId == handle.JobId ? handle : null;
            return jobReference is not null;
        }

        public bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle) => throw new NotSupportedException();

        public void RegisterJobType<TJob>(JobOptions<TJob> options, Guid jobTypeId, string jobTypeName)
            where TJob : class => throw new NotSupportedException();

        public Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Guid GetJobTypeId<TJob>()
            where TJob : class => throw new NotSupportedException();

        public void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator) =>
            throw new NotSupportedException();
    }
}
