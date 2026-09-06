using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

    private static ConsumeContext<CancelJobAttempt> CreateContext(CancelJobAttempt message)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Message = message;
        return context;
    }

    private interface TestConsumeContext : ConsumeContext<CancelJobAttempt>, ConsumeContext;

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

    private sealed record CancelJobAttemptMessage(Guid JobId, Guid AttemptId, string? Reason) : CancelJobAttempt;

    private sealed class RecordingJobHandle(Guid jobId, Guid attemptId) : JobHandle
    {
        public Guid JobId { get; } = jobId;
        public Guid AttemptId { get; } = attemptId;
        public Task Execution => Task.CompletedTask;
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
