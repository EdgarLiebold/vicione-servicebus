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
    public async Task SelectInstanceAsync_SelectsTheLeastLoadedEligibleInstance()
    {
        ConsumeContext<AllocateJobSlot> context = CreateContext(NewId.NextGuid());
        var info = new TestJobTypeInfo(concurrentJobLimit: 2);
        info.Instances.Add(FirstInstance, new JobTypeInstance { Used = DateTimeOffset.UnixEpoch });
        info.Instances.Add(SecondInstance, new JobTypeInstance { Used = DateTimeOffset.UnixEpoch.AddMinutes(1) });
        info.ActiveJobs.Add(new ActiveJob { JobId = NewId.NextGuid(), InstanceAddress = FirstInstance });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            context,
            info,
            TestContext.Current.CancellationToken);

        Assert.Equal(SecondInstance, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "least-recently-used-breaks-equal-load")]
    public async Task SelectInstanceAsync_PrefersTheLeastRecentlyUsedInstanceWhenLoadsMatch()
    {
        ConsumeContext<AllocateJobSlot> context = CreateContext(NewId.NextGuid());
        var info = new TestJobTypeInfo(concurrentJobLimit: 1);
        info.Instances.Add(FirstInstance, new JobTypeInstance { Used = DateTimeOffset.UnixEpoch.AddMinutes(2) });
        info.Instances.Add(SecondInstance, new JobTypeInstance { Used = DateTimeOffset.UnixEpoch });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            context,
            info,
            TestContext.Current.CancellationToken);

        Assert.Equal(SecondInstance, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "fully-allocated-instances-are-ineligible")]
    public async Task SelectInstanceAsync_ReturnsNullWhenEveryInstanceIsAtItsLimit()
    {
        ConsumeContext<AllocateJobSlot> context = CreateContext(NewId.NextGuid());
        var info = new TestJobTypeInfo(concurrentJobLimit: 1);
        info.Instances.Add(FirstInstance, new JobTypeInstance());
        info.Instances.Add(SecondInstance, new JobTypeInstance());
        info.ActiveJobs.Add(new ActiveJob { JobId = NewId.NextGuid(), InstanceAddress = FirstInstance });
        info.ActiveJobs.Add(new ActiveJob { JobId = NewId.NextGuid(), InstanceAddress = SecondInstance });

        Uri? actual = await DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
            context,
            info,
            TestContext.Current.CancellationToken);

        Assert.Null(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "required-inputs-and-cancellation-are-honored")]
    public async Task SelectInstanceAsync_RejectsMissingInputsAndCancellation()
    {
        ConsumeContext<AllocateJobSlot> context = CreateContext(NewId.NextGuid());
        var info = new TestJobTypeInfo(concurrentJobLimit: 1);

        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
                    null!,
                    info,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "jobTypeInfo",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(
                    context,
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DefaultJobDistributionStrategy.Instance.SelectInstanceAsync(context, info, canceled.Token));
        Assert.Equal(canceled.Token, exception.CancellationToken);
    }

    private static ConsumeContext<AllocateJobSlot> CreateContext(Guid jobId)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Message = new AllocateJobSlotMessage(jobId);
        return context;
    }

    private interface TestConsumeContext : ConsumeContext<AllocateJobSlot>, ConsumeContext;

    private class ConsumeContextProxy : DispatchProxy
    {
        public AllocateJobSlot Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name switch
            {
                "get_Message" => Message,
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
    }

    private sealed record AllocateJobSlotMessage(Guid JobId) : AllocateJobSlot
    {
        public Guid JobTypeId { get; init; }
        public TimeSpan JobTimeout { get; init; }
        public IReadOnlyDictionary<string, object>? JobProperties { get; init; }
    }

    private sealed class TestJobTypeInfo(int concurrentJobLimit) : JobTypeInfo
    {
        public string Name => "test-job";
        public int ConcurrentJobLimit { get; } = concurrentJobLimit;
        public IReadOnlyDictionary<string, object> JobTypeProperties { get; } = new Dictionary<string, object>();
        public List<ActiveJob> ActiveJobs { get; } = [];
        IReadOnlyList<ActiveJob> JobTypeInfo.ActiveJobs => ActiveJobs;
        public Dictionary<Uri, JobTypeInstance> Instances { get; } = [];
        IReadOnlyDictionary<Uri, JobTypeInstance> JobTypeInfo.Instances => Instances;
    }
}
