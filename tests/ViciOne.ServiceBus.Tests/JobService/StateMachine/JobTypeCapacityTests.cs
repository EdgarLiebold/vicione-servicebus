using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.StateMachine;

public sealed class JobTypeCapacityTests
{
    private static readonly Uri LiveInstance = new("loopback://localhost/live-job-service");
    private static readonly Uri ExpiredInstance = new("loopback://localhost/expired-job-service");
    private static readonly Uri MissingInstance = new("loopback://localhost/missing-job-service");
    private static readonly DateTimeOffset Now = new(2045, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CAPACITY", "expired-leases-deadlines-and-orphans-are-reconciled")]
    public void RemoveExpiredAllocations_PreservesOnlyLiveAllocationsOnKnownInstances()
    {
        Guid liveJobId = NewId.NextGuid();
        var saga = new JobTypeSaga
        {
            ActiveAllocationCount = 99,
            ServiceInstances = new Dictionary<Uri, JobServiceInstanceState>
            {
                [LiveInstance] = new() { LastHeartbeatAt = Now - TimeSpan.FromMinutes(1) },
                [ExpiredInstance] = new() { LastHeartbeatAt = Now - TimeSpan.FromMinutes(6) },
            },
            ActiveAllocations =
            [
                Allocation(liveJobId, LiveInstance, Now + TimeSpan.FromMinutes(1)),
                Allocation(NewId.NextGuid(), LiveInstance, Now),
                Allocation(NewId.NextGuid(), ExpiredInstance, Now + TimeSpan.FromMinutes(1)),
                Allocation(NewId.NextGuid(), MissingInstance, Now + TimeSpan.FromMinutes(1)),
            ],
        };

        JobTypeCapacity.RemoveExpiredAllocations(saga, Now, TimeSpan.FromMinutes(5));

        JobAllocationState remaining = Assert.Single(saga.ActiveAllocations);
        Assert.Equal(liveJobId, remaining.JobId);
        Assert.Equal(LiveInstance, remaining.InstanceAddress);
        Assert.Equal(1, saga.ActiveAllocationCount);
        Assert.Single(saga.ServiceInstances);
        Assert.Contains(LiveInstance, saga.ServiceInstances);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CAPACITY", "reconciliation-inputs-are-validated")]
    public void RemoveExpiredAllocations_RejectsMissingStateAndNonPositiveTimeouts()
    {
        Assert.Equal("saga", Assert.Throws<ArgumentNullException>(() =>
            JobTypeCapacity.RemoveExpiredAllocations(null!, Now, TimeSpan.FromMinutes(1))).ParamName);
        Assert.Equal("heartbeatTimeout", Assert.Throws<ArgumentOutOfRangeException>(() =>
            JobTypeCapacity.RemoveExpiredAllocations(new JobTypeSaga(), Now, TimeSpan.Zero)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CAPACITY", "allocation-equality-is-the-job-identity-contract")]
    public void JobAllocationState_UsesOnlyJobIdentityForEqualityAndHashing()
    {
        Guid jobId = NewId.NextGuid();
        var first = new JobAllocationState
        {
            JobId = jobId,
            InstanceAddress = LiveInstance,
            ExpiresAt = Now,
        };
        var equivalent = new JobAllocationState
        {
            JobId = jobId,
            InstanceAddress = ExpiredInstance,
            ExpiresAt = Now.AddDays(1),
        };
        var different = new JobAllocationState
        {
            JobId = NewId.NextGuid(),
            InstanceAddress = LiveInstance,
            ExpiresAt = Now,
        };

        Assert.True(first.Equals(first));
        Assert.True(first.Equals(equivalent));
        Assert.True(first.Equals((object)equivalent));
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.False(first.Equals(different));
        Assert.False(first.Equals((JobAllocationState?)null));
        Assert.False(first.Equals(new object()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "custom-strategy-view-cannot-mutate-persisted-state")]
    public void JobDistributionContext_IsImmutableAndIsolatedFromPersistedState()
    {
        Guid jobId = NewId.NextGuid();
        var saga = new JobTypeSaga
        {
            Name = "invoice-job",
            JobTypeProperties = new Dictionary<string, object> { ["Tier"] = "gold" },
            ServiceInstances = new Dictionary<Uri, JobServiceInstanceState>
            {
                [LiveInstance] = new()
                {
                    LastAllocationAt = Now,
                    Properties = new Dictionary<string, object> { ["Region"] = "west" },
                },
            },
            ActiveAllocations =
            [
                new JobAllocationState
                {
                    JobId = jobId,
                    InstanceAddress = LiveInstance,
                    Properties = new Dictionary<string, object> { ["Tenant"] = "one" },
                },
            ],
        };
        var context = new JobDistributionContext(saga);

        saga.ActiveAllocations[0].JobId = NewId.NextGuid();
        saga.ActiveAllocations[0].Properties!["Tenant"] = "two";
        saga.ServiceInstances[LiveInstance].LastAllocationAt = Now.AddDays(1);
        saga.ServiceInstances[LiveInstance].Properties!["Region"] = "east";
        saga.JobTypeProperties["Tier"] = "silver";
        saga.ActiveAllocations.Clear();
        saga.ServiceInstances.Clear();

        JobAllocationInfo allocation = Assert.Single(context.ActiveAllocations);
        Assert.Equal(jobId, allocation.JobId);
        Assert.Equal("one", allocation.JobProperties["Tenant"]);
        Assert.Equal(Now, context.ServiceInstances[LiveInstance].LastAllocationAt);
        Assert.Equal("west", context.ServiceInstances[LiveInstance].InstanceProperties["Region"]);
        Assert.Equal("gold", context.JobTypeProperties["Tier"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object>)context.JobTypeProperties)["Tier"] = "bronze");
        Assert.Throws<NotSupportedException>(() => ((IList<JobAllocationInfo>)context.ActiveAllocations).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Uri, JobServiceInstanceInfo>)context.ServiceInstances).Clear());
    }

    [Theory]
    [InlineData(InvalidConcurrencyUpdate.EmptyJobType)]
    [InlineData(InvalidConcurrencyUpdate.MissingInstanceAddress)]
    [InlineData(InvalidConcurrencyUpdate.UndefinedKind)]
    [InlineData(InvalidConcurrencyUpdate.NonPositiveLimit)]
    [InlineData(InvalidConcurrencyUpdate.NonPositiveGlobalLimit)]
    [InlineData(InvalidConcurrencyUpdate.MissingJobTypeName)]
    [InlineData(InvalidConcurrencyUpdate.NonPositiveOverrideDuration)]
    [RequirementCoverage("REQ-VSB-JOB-CAPACITY", "invalid-concurrency-updates-are-rejected-before-mutation")]
    public void ValidateConcurrencyUpdate_RejectsEveryInvalidInvariant(InvalidConcurrencyUpdate invalid)
    {
        var message = ValidConfigurationUpdate();
        switch (invalid)
        {
            case InvalidConcurrencyUpdate.EmptyJobType: message.JobTypeId = Guid.Empty; break;
            case InvalidConcurrencyUpdate.MissingInstanceAddress: message.InstanceAddress = null!; break;
            case InvalidConcurrencyUpdate.UndefinedKind: message.UpdateKind = (JobConcurrencyUpdateKind)99; break;
            case InvalidConcurrencyUpdate.NonPositiveLimit: message.ConcurrentJobLimit = 0; break;
            case InvalidConcurrencyUpdate.NonPositiveGlobalLimit: message.GlobalConcurrentJobLimit = 0; break;
            case InvalidConcurrencyUpdate.MissingJobTypeName: message.JobTypeName = " "; break;
            case InvalidConcurrencyUpdate.NonPositiveOverrideDuration:
                message.UpdateKind = JobConcurrencyUpdateKind.TemporaryOverride;
                message.Duration = TimeSpan.Zero;
                break;
        }

        MessageException exception = Assert.Throws<MessageException>(() =>
            JobTypeStateMachineBehaviorExtensions.ValidateConcurrencyUpdate(message));

        Assert.Equal(typeof(SetConcurrentJobLimit), exception.MessageType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CAPACITY", "all-defined-concurrency-update-kinds-accept-valid-values")]
    public void ValidateConcurrencyUpdate_AcceptsEveryDefinedUpdateKind()
    {
        SetConcurrentJobLimitMessage configuration = ValidConfigurationUpdate();
        SetConcurrentJobLimitMessage temporaryOverride = ValidConfigurationUpdate();
        temporaryOverride.UpdateKind = JobConcurrencyUpdateKind.TemporaryOverride;
        temporaryOverride.Duration = TimeSpan.FromMinutes(2);
        SetConcurrentJobLimitMessage heartbeat = ValidConfigurationUpdate();
        heartbeat.UpdateKind = JobConcurrencyUpdateKind.Heartbeat;
        SetConcurrentJobLimitMessage stopped = ValidConfigurationUpdate();
        stopped.UpdateKind = JobConcurrencyUpdateKind.InstanceStopped;

        JobTypeStateMachineBehaviorExtensions.ValidateConcurrencyUpdate(configuration);
        JobTypeStateMachineBehaviorExtensions.ValidateConcurrencyUpdate(temporaryOverride);
        JobTypeStateMachineBehaviorExtensions.ValidateConcurrencyUpdate(heartbeat);
        JobTypeStateMachineBehaviorExtensions.ValidateConcurrencyUpdate(stopped);
    }

    private static JobAllocationState Allocation(Guid jobId, Uri instanceAddress, DateTimeOffset deadline) => new()
    {
        JobId = jobId,
        InstanceAddress = instanceAddress,
        ExpiresAt = deadline,
    };

    private static SetConcurrentJobLimitMessage ValidConfigurationUpdate() => new()
    {
        JobTypeId = NewId.NextGuid(),
        InstanceAddress = LiveInstance,
        ConcurrentJobLimit = 2,
        UpdateKind = JobConcurrencyUpdateKind.Configuration,
        JobTypeName = "invoice-job",
    };

    public enum InvalidConcurrencyUpdate
    {
        EmptyJobType,
        MissingInstanceAddress,
        UndefinedKind,
        NonPositiveLimit,
        NonPositiveGlobalLimit,
        MissingJobTypeName,
        NonPositiveOverrideDuration,
    }

    private sealed class SetConcurrentJobLimitMessage : SetConcurrentJobLimit
    {
        public Guid JobTypeId { get; set; }
        public Uri InstanceAddress { get; set; } = null!;
        public int ConcurrentJobLimit { get; set; }
        public JobConcurrencyUpdateKind UpdateKind { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? JobTypeName { get; set; }
        public IReadOnlyDictionary<string, object>? JobTypeProperties { get; set; }
        public IReadOnlyDictionary<string, object>? InstanceProperties { get; set; }
        public int? GlobalConcurrentJobLimit { get; set; }
    }
}
