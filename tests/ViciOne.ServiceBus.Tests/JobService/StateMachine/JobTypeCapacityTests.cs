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
            ActiveJobCount = 99,
            Instances = new Dictionary<Uri, JobTypeInstance>
            {
                [LiveInstance] = new() { Updated = Now - TimeSpan.FromMinutes(1) },
                [ExpiredInstance] = new() { Updated = Now - TimeSpan.FromMinutes(6) },
            },
            ActiveJobs =
            [
                Allocation(liveJobId, LiveInstance, Now + TimeSpan.FromMinutes(1)),
                Allocation(NewId.NextGuid(), LiveInstance, Now),
                Allocation(NewId.NextGuid(), ExpiredInstance, Now + TimeSpan.FromMinutes(1)),
                Allocation(NewId.NextGuid(), MissingInstance, Now + TimeSpan.FromMinutes(1)),
            ],
        };

        JobTypeCapacity.RemoveExpiredAllocations(saga, Now, TimeSpan.FromMinutes(5));

        ActiveJob remaining = Assert.Single(saga.ActiveJobs);
        Assert.Equal(liveJobId, remaining.JobId);
        Assert.Equal(LiveInstance, remaining.InstanceAddress);
        Assert.Equal(1, saga.ActiveJobCount);
        Assert.Single(saga.Instances);
        Assert.Contains(LiveInstance, saga.Instances);
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
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "coordinator-snapshots-replace-and-isolate-metadata")]
    public void CopyProperties_CreatesAnIndependentCaseInsensitiveSnapshot()
    {
        var source = new Dictionary<string, object> { ["Region"] = "west" };

        Dictionary<string, object> copy = JobTypeCapacity.CopyProperties(source);
        source["Region"] = "east";

        Assert.Equal("west", copy["region"]);
        Assert.Empty(JobTypeCapacity.CopyProperties(null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "custom-strategy-view-cannot-mutate-persisted-state")]
    public void JobTypeInfoSnapshot_IsolatesPersistedStateFromStrategyMutation()
    {
        Guid jobId = NewId.NextGuid();
        var saga = new JobTypeSaga
        {
            Name = "invoice-job",
            JobTypeProperties = new Dictionary<string, object> { ["Tier"] = "gold" },
            Instances = new Dictionary<Uri, JobTypeInstance>
            {
                [LiveInstance] = new()
                {
                    Used = Now,
                    InstanceProperties = new Dictionary<string, object> { ["Region"] = "west" },
                },
            },
            ActiveJobs =
            [
                new ActiveJob
                {
                    JobId = jobId,
                    InstanceAddress = LiveInstance,
                    JobProperties = new Dictionary<string, object> { ["Tenant"] = "one" },
                },
            ],
        };
        var snapshot = new JobTypeInfoSnapshot(saga);

        snapshot.ActiveJobs[0].JobId = NewId.NextGuid();
        snapshot.ActiveJobs[0].JobProperties!["Tenant"] = "two";
        snapshot.Instances[LiveInstance].Used = Now.AddDays(1);
        snapshot.Instances[LiveInstance].InstanceProperties!["Region"] = "east";
        ((Dictionary<string, object>)snapshot.JobTypeProperties)["Tier"] = "silver";

        Assert.Equal(jobId, saga.ActiveJobs[0].JobId);
        Assert.Equal("one", saga.ActiveJobs[0].JobProperties!["Tenant"]);
        Assert.Equal(Now, saga.Instances[LiveInstance].Used);
        Assert.Equal("west", saga.Instances[LiveInstance].InstanceProperties!["Region"]);
        Assert.Equal("gold", saga.JobTypeProperties["Tier"]);
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

    private static ActiveJob Allocation(Guid jobId, Uri instanceAddress, DateTimeOffset deadline) => new()
    {
        JobId = jobId,
        InstanceAddress = instanceAddress,
        Deadline = deadline,
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
