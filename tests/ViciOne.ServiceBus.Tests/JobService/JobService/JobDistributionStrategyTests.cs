using System.Reflection;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobDistributionStrategyTests
{
    private static readonly Uri FirstInstance = new("loopback://localhost/job-instance-1");
    private static readonly Uri SecondInstance = new("loopback://localhost/job-instance-2");

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "least-loaded-eligible-instance-is-selected")]
    public async Task SelectInstance_SelectsTheLeastLoadedEligibleInstanceAsync()
    {
        ConsumeContext<IAllocateJobSlot> requestContext = CreateRequestContext(NewId.NextGuid());
        JobDistributionContext distributionContext = CreateDistributionContext(
            concurrentJobLimit: 2,
            new Dictionary<Uri, JobServiceInstanceState>
            {
                [FirstInstance] = new() { LastAllocationAt = DateTimeOffset.UnixEpoch },
                [SecondInstance] = new() { LastAllocationAt = DateTimeOffset.UnixEpoch.AddMinutes(1) },
            },
            new JobAllocationState { JobId = NewId.NextGuid(), InstanceAddress = FirstInstance });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            requestContext,
            distributionContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(SecondInstance, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "least-recently-used-breaks-equal-load")]
    public async Task SelectInstance_PrefersTheLeastRecentlyUsedInstanceWhenLoadsMatchAsync()
    {
        ConsumeContext<IAllocateJobSlot> requestContext = CreateRequestContext(NewId.NextGuid());
        JobDistributionContext distributionContext = CreateDistributionContext(
            concurrentJobLimit: 1,
            new Dictionary<Uri, JobServiceInstanceState>
            {
                [FirstInstance] = new() { LastAllocationAt = DateTimeOffset.UnixEpoch.AddMinutes(2) },
                [SecondInstance] = new() { LastAllocationAt = DateTimeOffset.UnixEpoch },
            });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            requestContext,
            distributionContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(SecondInstance, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "fully-allocated-instances-are-ineligible")]
    public async Task SelectInstance_ReturnsNullWhenEveryInstanceIsAtItsLimitAsync()
    {
        ConsumeContext<IAllocateJobSlot> requestContext = CreateRequestContext(NewId.NextGuid());
        JobDistributionContext distributionContext = CreateDistributionContext(
            concurrentJobLimit: 1,
            new Dictionary<Uri, JobServiceInstanceState>
            {
                [FirstInstance] = new(),
                [SecondInstance] = new(),
            },
            new JobAllocationState { JobId = NewId.NextGuid(), InstanceAddress = FirstInstance },
            new JobAllocationState { JobId = NewId.NextGuid(), InstanceAddress = SecondInstance });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            requestContext,
            distributionContext,
            TestContext.Current.CancellationToken);

        Assert.Null(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "required-inputs-and-cancellation-are-honored")]
    public async Task SelectInstance_RejectsMissingInputsAndCancellationAsync()
    {
        ConsumeContext<IAllocateJobSlot> requestContext = CreateRequestContext(NewId.NextGuid());
        JobDistributionContext distributionContext = CreateDistributionContext(
            concurrentJobLimit: 1,
            new Dictionary<Uri, JobServiceInstanceState>());

        Assert.Equal(
            "requestContext",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
                    null!,
                    distributionContext,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "distributionContext",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
                    requestContext,
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(requestContext, distributionContext, canceled.Token));
        Assert.Equal(canceled.Token, exception.CancellationToken);
    }

    private static ConsumeContext<IAllocateJobSlot> CreateRequestContext(Guid jobId)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Message = new AllocateJobSlotMessage(jobId);
        return context;
    }

    private static JobDistributionContext CreateDistributionContext(
        int concurrentJobLimit,
        Dictionary<Uri, JobServiceInstanceState> serviceInstances,
        params JobAllocationState[] activeAllocations)
    {
        return new JobDistributionContext(new JobTypeSaga
        {
            Name = "test-job",
            ConcurrentJobLimit = concurrentJobLimit,
            ServiceInstances = serviceInstances,
            ActiveAllocations = [.. activeAllocations],
        });
    }

    private interface TestConsumeContext : ConsumeContext<IAllocateJobSlot>, ConsumeContext;

    private class ConsumeContextProxy : DispatchProxy
    {
        public IAllocateJobSlot Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_Message" => Message,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private sealed record AllocateJobSlotMessage(Guid JobId) : IAllocateJobSlot
    {
        public Guid JobTypeId { get; init; }
        public TimeSpan JobTimeout { get; init; }
        public IReadOnlyDictionary<string, object>? JobProperties { get; init; }
    }

}
