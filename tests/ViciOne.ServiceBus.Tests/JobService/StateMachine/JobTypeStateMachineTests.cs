using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.StateMachine;

public sealed class JobTypeStateMachineTests
{
    private static readonly Uri LiveInstance = new("loopback://localhost/live-job-service");
    private static readonly Uri UnknownInstance = new("loopback://localhost/unknown-job-service");
    private static readonly DateTimeOffset Now = new(2045, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-TYPE-STATE", "configuration-heartbeat-override-and-stop-preserve-exact-state")]
    public async Task ConcurrencyUpdates_PreserveTheCompleteInstanceAndLimitStateAsync()
    {
        var machine = new JobTypeStateMachine();
        Guid jobTypeId = NewId.NextGuid();
        var saga = new JobTypeSaga { CorrelationId = jobTypeId };
        var instanceProperties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Region"] = "west",
            ["region"] = "north",
        };
        var jobTypeProperties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Tier"] = "silver",
            ["tier"] = "gold",
        };

        await RaiseAsync(machine, saga, machine.SetConcurrentJobLimit, new SetConcurrentJobLimitCommand
        {
            JobTypeId = jobTypeId,
            InstanceAddress = LiveInstance,
            ConcurrentJobLimit = 3,
            GlobalConcurrentJobLimit = 7,
            UpdateKind = JobConcurrencyUpdateKind.Configuration,
            JobTypeName = "invoice-job",
            JobTypeProperties = jobTypeProperties,
            InstanceProperties = instanceProperties,
        }, Now);

        AssertState(machine, saga, machine.Idle);
        Assert.Equal(3, saga.ConcurrentJobLimit);
        Assert.Equal(7, saga.GlobalConcurrentJobLimit);
        Assert.Equal("invoice-job", saga.Name);
        Assert.Single(saga.JobTypeProperties);
        Assert.Equal("gold", saga.JobTypeProperties["TIER"]);
        JobServiceInstanceState instance = Assert.Single(saga.ServiceInstances).Value;
        Assert.Equal(Now, instance.LastHeartbeatAt);
        Dictionary<string, object> storedInstanceProperties = Assert.IsType<Dictionary<string, object>>(instance.Properties);
        Assert.Single(storedInstanceProperties);
        Assert.Equal("north", storedInstanceProperties["REGION"]);

        instanceProperties["region"] = "mutated";
        jobTypeProperties["tier"] = "mutated";
        Assert.Equal("north", storedInstanceProperties["region"]);
        Assert.Equal("gold", saga.JobTypeProperties["tier"]);

        DateTimeOffset heartbeatAt = Now.AddMinutes(1);
        await RaiseAsync(machine, saga, machine.SetConcurrentJobLimit, new SetConcurrentJobLimitCommand
        {
            JobTypeId = jobTypeId,
            InstanceAddress = LiveInstance,
            UpdateKind = JobConcurrencyUpdateKind.Heartbeat,
            InstanceProperties = new Dictionary<string, object> { ["region"] = "east" },
        }, heartbeatAt);

        Assert.Equal(heartbeatAt, instance.LastHeartbeatAt);
        Assert.Equal("east", instance.Properties!["REGION"]);

        DateTimeOffset overrideAt = Now.AddMinutes(2);
        await RaiseAsync(machine, saga, machine.SetConcurrentJobLimit, new SetConcurrentJobLimitCommand
        {
            JobTypeId = jobTypeId,
            InstanceAddress = LiveInstance,
            ConcurrentJobLimit = 5,
            UpdateKind = JobConcurrencyUpdateKind.TemporaryOverride,
        }, overrideAt);

        Assert.Equal(5, saga.OverrideConcurrentJobLimit);
        Assert.Equal(overrideAt.AddMinutes(30), saga.OverrideExpiresAt);

        await RaiseAsync(machine, saga, machine.SetConcurrentJobLimit, new SetConcurrentJobLimitCommand
        {
            JobTypeId = jobTypeId,
            InstanceAddress = LiveInstance,
            UpdateKind = JobConcurrencyUpdateKind.InstanceStopped,
        }, Now.AddMinutes(3));

        Assert.Empty(saga.ServiceInstances);
        AssertState(machine, saga, machine.Idle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-TYPE-STATE", "allocation-is-idempotent-and-global-capacity-is-enforced")]
    public async Task Allocation_IsIdempotentAndEnforcesGlobalCapacityAsync()
    {
        var machine = new JobTypeStateMachine();
        var saga = CreateAvailableSaga();
        saga.GlobalConcurrentJobLimit = 1;
        saga.OverrideConcurrentJobLimit = 9;
        saga.OverrideExpiresAt = Now;
        await SetStateAsync(machine, saga, machine.Idle);
        var strategy = new FixedDistributionStrategy(LiveInstance);
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IJobDistributionStrategy>(strategy)
            .BuildServiceProvider();
        Guid jobId = NewId.NextGuid();
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Tenant"] = "one",
            ["tenant"] = "two",
        };

        var firstOutgoing = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.JobSlotRequested, new AllocateJobSlotCommand
        {
            JobId = jobId,
            JobTypeId = saga.CorrelationId,
            JobTimeout = TimeSpan.FromMinutes(4),
            JobProperties = properties,
        }, Now, firstOutgoing, provider);

        AssertState(machine, saga, machine.Active);
        Assert.Null(saga.OverrideConcurrentJobLimit);
        Assert.Null(saga.OverrideExpiresAt);
        JobAllocationState allocation = Assert.Single(saga.ActiveAllocations);
        Assert.Equal(jobId, allocation.JobId);
        Assert.Equal(LiveInstance, allocation.InstanceAddress);
        Assert.Equal(Now.AddMinutes(4), allocation.ExpiresAt);
        Dictionary<string, object> storedJobProperties = Assert.IsType<Dictionary<string, object>>(allocation.Properties);
        Assert.Single(storedJobProperties);
        Assert.Equal("two", storedJobProperties["TENANT"]);
        Assert.Equal(Now, saga.ServiceInstances[LiveInstance].LastAllocationAt);
        Assert.Equal(1, saga.ActiveAllocationCount);
        Assert.Equal(1, strategy.CallCount);
        Assert.Equal(LiveInstance, Assert.Single(firstOutgoing.Messages.OfType<JobSlotAllocated>()).InstanceAddress);

        properties["tenant"] = "mutated";
        Assert.Equal("two", storedJobProperties["tenant"]);

        var duplicateOutgoing = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.JobSlotRequested, new AllocateJobSlotCommand
        {
            JobId = jobId,
            JobTypeId = saga.CorrelationId,
            JobTimeout = TimeSpan.FromHours(1),
        }, Now.AddSeconds(1), duplicateOutgoing, provider);

        Assert.Single(saga.ActiveAllocations);
        Assert.Equal(1, strategy.CallCount);
        Assert.Equal(LiveInstance, Assert.Single(duplicateOutgoing.Messages.OfType<JobSlotAllocated>()).InstanceAddress);

        var unavailableOutgoing = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.JobSlotRequested, new AllocateJobSlotCommand
        {
            JobId = NewId.NextGuid(),
            JobTypeId = saga.CorrelationId,
            JobTimeout = TimeSpan.FromMinutes(1),
        }, Now.AddSeconds(2), unavailableOutgoing, provider);

        Assert.Single(saga.ActiveAllocations);
        Assert.Equal(1, strategy.CallCount);
        Assert.Equal(saga.ActiveAllocations[0].JobId,
            Assert.Single(duplicateOutgoing.Messages.OfType<JobSlotAllocated>()).JobId);
        Assert.Single(unavailableOutgoing.Messages.OfType<JobSlotUnavailable>());
    }

    [Theory]
    [InlineData(DistributionProvider.Scope)]
    [InlineData(DistributionProvider.ServiceProvider)]
    [RequirementCoverage("REQ-VSB-JOB-DISTRIBUTION", "scoped-and-provider-strategies-control-allocation")]
    public async Task RegisteredDistributionStrategy_ControlsAllocationAsync(DistributionProvider providerKind)
    {
        var machine = new JobTypeStateMachine();
        var saga = CreateAvailableSaga();
        await SetStateAsync(machine, saga, machine.Idle);
        var strategy = new FixedDistributionStrategy(UnknownInstance);
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IJobDistributionStrategy>(strategy)
            .BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.JobSlotRequested, new AllocateJobSlotCommand
        {
            JobId = NewId.NextGuid(),
            JobTypeId = saga.CorrelationId,
            JobTimeout = TimeSpan.FromMinutes(1),
        }, Now, outgoing,
            providerKind == DistributionProvider.ServiceProvider ? provider : null,
            providerKind == DistributionProvider.Scope ? scope : null);

        AssertState(machine, saga, machine.Idle);
        Assert.Empty(saga.ActiveAllocations);
        Assert.Equal(1, strategy.CallCount);
        Assert.Single(outgoing.Messages.OfType<JobSlotUnavailable>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-TYPE-STATE", "suspect-release-removes-allocation-and-instance")]
    public async Task SuspectRelease_RemovesTheAllocationAndItsInstanceAsync()
    {
        var machine = new JobTypeStateMachine();
        var saga = CreateAvailableSaga();
        Guid jobId = NewId.NextGuid();
        saga.ActiveAllocations.Add(new JobAllocationState
        {
            JobId = jobId,
            InstanceAddress = LiveInstance,
            ExpiresAt = Now.AddMinutes(1),
        });
        saga.ActiveAllocationCount = 1;
        await SetStateAsync(machine, saga, machine.Active);

        await RaiseAsync(machine, saga, machine.JobSlotReleased, new JobSlotReleasedEvent
        {
            JobId = jobId,
            JobTypeId = saga.CorrelationId,
            Disposition = JobSlotDisposition.Suspect,
        }, Now);

        AssertState(machine, saga, machine.Idle);
        Assert.Empty(saga.ActiveAllocations);
        Assert.Equal(0, saga.ActiveAllocationCount);
        Assert.Empty(saga.ServiceInstances);
    }

    private static JobTypeSaga CreateAvailableSaga() => new()
    {
        CorrelationId = NewId.NextGuid(),
        ConcurrentJobLimit = 2,
        ServiceInstances = new Dictionary<Uri, JobServiceInstanceState>
        {
            [LiveInstance] = new()
            {
                LastHeartbeatAt = Now,
                Properties = new Dictionary<string, object>(),
            },
        },
    };

    private static JobServiceOptions CreateSettings() => new()
    {
        JobTypeEndpointName = "job-type",
        JobEndpointName = "job",
        JobAttemptEndpointName = "job-attempt",
        JobSagaEndpointAddress = new Uri("loopback://localhost/job-saga"),
        JobTypeSagaEndpointAddress = new Uri("loopback://localhost/job-type-saga"),
        JobAttemptSagaEndpointAddress = new Uri("loopback://localhost/job-attempt-saga"),
        HeartbeatTimeout = TimeSpan.FromMinutes(5),
    };

    private static async Task RaiseAsync<T>(
        JobTypeStateMachine machine,
        JobTypeSaga saga,
        IEvent<T> @event,
        T message,
        DateTimeOffset now,
        OutgoingMessageRecorder? outgoing = null,
        IServiceProvider? serviceProvider = null,
        IServiceScope? serviceScope = null)
        where T : class
    {
        outgoing ??= new OutgoingMessageRecorder();
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoing,
            sentTime: now,
            responseAddress: new Uri("loopback://localhost/job-saga"),
            requestId: NewId.NextGuid(),
            serviceProvider: serviceProvider);
        consumeContext.SetTimeProvider(new FixedTimeProvider(now));
        if (serviceScope is not null)
            consumeContext.AddOrUpdatePayload(() => serviceScope, _ => serviceScope);
        var instance = new SagaInstance<JobTypeSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobTypeSaga, T>(consumeContext, instance);
        JobServiceOptions settings = CreateSettings();
        sagaContext.AddOrUpdatePayload<JobSagaSettings>(() => settings, _ => settings);
        IBehaviorContext<JobTypeSaga, T> behaviorContext =
            new ViciOneServiceBusStateMachine<JobTypeSaga>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await ((IStateMachine<JobTypeSaga>)machine).RaiseEventAsync(behaviorContext);
    }

    private static async Task SetStateAsync(JobTypeStateMachine machine, JobTypeSaga saga, IState state)
    {
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StateSetupMessage(),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<JobTypeSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobTypeSaga, StateSetupMessage>(consumeContext, instance);
        IBehaviorContext<JobTypeSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<JobTypeSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await machine.Accessor.SetAsync(behaviorContext, machine.GetState(state.Name));
    }

    private static void AssertState(JobTypeStateMachine machine, JobTypeSaga saga, IState expected) =>
        Assert.True(machine.Accessor.GetStateExpression(expected).Compile()(saga), $"Expected state {expected.Name}.");

    private sealed class FixedDistributionStrategy(Uri? selectedInstance) : IJobDistributionStrategy
    {
        public int CallCount { get; private set; }

        public Task<Uri?> SelectInstanceAsync(
            ConsumeContext<AllocateJobSlot> requestContext,
            JobDistributionContext distributionContext,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requestContext);
            ArgumentNullException.ThrowIfNull(distributionContext);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(selectedInstance);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public sealed record StateSetupMessage;

    public enum DistributionProvider
    {
        Scope,
        ServiceProvider,
    }
}
