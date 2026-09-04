using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.StateMachine;

public sealed class JobAttemptGenerationTests
{
    [Theory]
    [InlineData(JobSagaState.Submitted)]
    [InlineData(JobSagaState.WaitingToStart)]
    [InlineData(JobSagaState.WaitingToRetry)]
    [InlineData(JobSagaState.WaitingForSlot)]
    [InlineData(JobSagaState.StartingJobAttempt)]
    [InlineData(JobSagaState.Started)]
    [InlineData(JobSagaState.Completed)]
    [InlineData(JobSagaState.Faulted)]
    [InlineData(JobSagaState.Canceled)]
    [InlineData(JobSagaState.AllocatingJobSlot)]
    [InlineData(JobSagaState.CancellationPending)]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-GENERATION", "stale-attempt-events-preserve-every-state")]
    public async Task StaleAttemptEvents_LeaveTheCompleteSagaSnapshotUnchangedAsync(JobSagaState sagaState)
    {
        var machine = new JobStateMachine();
        Guid currentAttemptId = NewId.NextGuid();
        var saga = new JobSaga
        {
            CorrelationId = NewId.NextGuid(),
            AttemptId = currentAttemptId,
            RetryAttempt = 1,
            Submitted = new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
            Started = new DateTime(2026, 8, 15, 8, 1, 0, DateTimeKind.Utc),
            ServiceAddress = new Uri("loopback://localhost/job-service"),
            Job = new Dictionary<string, object> { ["label"] = "current-attempt" },
            JobTypeId = NewId.NextGuid(),
            LastProgressValue = 41,
            LastProgressLimit = 100,
            LastProgressSequenceNumber = 7,
            JobState = new Dictionary<string, object> { ["checkpoint"] = "retained" },
            JobProperties = new Dictionary<string, object> { ["owner"] = "native" },
        };
        State expectedState = GetState(machine, sagaState);
        await SetStateAsync(machine, saga, expectedState);
        JobSagaSnapshot expected = Snapshot(saga);

        foreach ((StaleAttemptEvent kind, object message) in CreateStaleEvents(saga))
        {
            await RaiseAsync(machine, saga, kind, message);

            Assert.Equal(expected, Snapshot(saga));
            Assert.True(machine.Accessor.GetStateExpression(expectedState).Compile()(saga),
                $"{kind} moved the saga away from {expectedState.Name}");
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-GENERATION", "current-attempt-keeps-state-rules")]
    public async Task CurrentAttemptEvent_StillUsesTheConfiguredStateRuleAsync()
    {
        var machine = new JobStateMachine();
        var saga = new JobSaga
        {
            CorrelationId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            ServiceAddress = new Uri("loopback://localhost/job-service"),
            Job = new Dictionary<string, object>(),
            JobTypeId = NewId.NextGuid(),
        };
        var message = new JobAttemptStartedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc),
            InstanceAddress = new Uri("loopback://localhost/current-instance"),
        };
        await SetStateAsync(machine, saga, machine.Submitted);

        await Assert.ThrowsAsync<UnhandledEventException>(() =>
            RaiseAsync(machine, saga, machine.AttemptStarted, message));
    }

    private static IEnumerable<(StaleAttemptEvent Kind, object Message)> CreateStaleEvents(JobSaga saga)
    {
        Guid staleAttemptId = NewId.NextGuid();

        yield return (StaleAttemptEvent.Started, (JobAttemptStarted)new JobAttemptStartedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc),
            InstanceAddress = new Uri("loopback://localhost/old-instance"),
        });
        yield return (StaleAttemptEvent.Completed, (JobAttemptCompleted)new JobAttemptCompletedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromMinutes(1),
        });
        yield return (StaleAttemptEvent.Faulted, (JobAttemptFaulted)new JobAttemptFaultedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
        });
        yield return (StaleAttemptEvent.Canceled, (JobAttemptCanceled)new JobAttemptCanceledEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
            Reason = "late cancellation from the previous attempt",
        });

        StartJobAttempt command = new StartJobAttemptCommand
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            ServiceAddress = saga.ServiceAddress,
            InstanceAddress = new Uri("loopback://localhost/old-instance"),
            Job = saga.Job,
            JobTypeId = saga.JobTypeId,
        };
        yield return (StaleAttemptEvent.StartFaulted, (Fault<StartJobAttempt>)new FaultEvent<StartJobAttempt>
        {
            FaultId = NewId.NextGuid(),
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
            Message = command,
            Exceptions = [],
        });
    }

    private static Task RaiseAsync(JobStateMachine machine, JobSaga saga, StaleAttemptEvent kind, object message) =>
        kind switch
        {
            StaleAttemptEvent.Started => RaiseAsync(machine, saga, machine.AttemptStarted, (JobAttemptStarted)message),
            StaleAttemptEvent.Completed => RaiseAsync(machine, saga, machine.AttemptCompleted, (JobAttemptCompleted)message),
            StaleAttemptEvent.Faulted => RaiseAsync(machine, saga, machine.AttemptFaulted, (JobAttemptFaulted)message),
            StaleAttemptEvent.Canceled => RaiseAsync(machine, saga, machine.AttemptCanceled, (JobAttemptCanceled)message),
            StaleAttemptEvent.StartFaulted => RaiseAsync(machine, saga, machine.StartJobAttemptFaulted, (Fault<StartJobAttempt>)message),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static async Task RaiseAsync<T>(JobStateMachine machine, JobSaga saga, Event<T> @event, T message)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, T>(consumeContext, instance);
        BehaviorContext<JobSaga, T> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await ((StateMachine<JobSaga>)machine).RaiseEventAsync(behaviorContext);
    }

    private static async Task SetStateAsync(JobStateMachine machine, JobSaga saga, State state)
    {
        var message = new StateSetupMessage();
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, StateSetupMessage>(consumeContext, instance);
        BehaviorContext<JobSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await machine.Accessor.SetAsync(behaviorContext, machine.GetState(state.Name));
    }

    private static State GetState(JobStateMachine machine, JobSagaState state) =>
        state switch
        {
            JobSagaState.Submitted => machine.Submitted,
            JobSagaState.WaitingToStart => machine.WaitingToStart,
            JobSagaState.WaitingToRetry => machine.WaitingToRetry,
            JobSagaState.WaitingForSlot => machine.WaitingForSlot,
            JobSagaState.StartingJobAttempt => machine.StartingJobAttempt,
            JobSagaState.Started => machine.Started,
            JobSagaState.Completed => machine.Completed,
            JobSagaState.Faulted => machine.Faulted,
            JobSagaState.Canceled => machine.Canceled,
            JobSagaState.AllocatingJobSlot => machine.AllocatingJobSlot,
            JobSagaState.CancellationPending => machine.CancellationPending,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    private static JobSagaSnapshot Snapshot(JobSaga saga) =>
        new(
            saga.CurrentState,
            saga.Submitted,
            saga.ServiceAddress,
            saga.JobTimeout,
            saga.Job,
            saga.JobTypeId,
            saga.AttemptId,
            saga.RetryAttempt,
            saga.Started,
            saga.Completed,
            saga.Duration,
            saga.Faulted,
            saga.Reason,
            saga.LastProgressValue,
            saga.LastProgressLimit,
            saga.LastProgressSequenceNumber,
            saga.JobState,
            saga.JobProperties);

    public enum JobSagaState
    {
        Submitted,
        WaitingToStart,
        WaitingToRetry,
        WaitingForSlot,
        StartingJobAttempt,
        Started,
        Completed,
        Faulted,
        Canceled,
        AllocatingJobSlot,
        CancellationPending,
    }

    private enum StaleAttemptEvent
    {
        Started,
        Completed,
        Faulted,
        Canceled,
        StartFaulted,
    }

    public sealed record StateSetupMessage;

    private sealed record JobSagaSnapshot(
        int CurrentState,
        DateTimeOffset? Submitted,
        Uri ServiceAddress,
        TimeSpan? JobTimeout,
        Dictionary<string, object> Job,
        Guid JobTypeId,
        Guid AttemptId,
        int RetryAttempt,
        DateTimeOffset? Started,
        DateTimeOffset? Completed,
        TimeSpan? Duration,
        DateTimeOffset? Faulted,
        string? Reason,
        long? LastProgressValue,
        long? LastProgressLimit,
        long? LastProgressSequenceNumber,
        Dictionary<string, object>? JobState,
        Dictionary<string, object> JobProperties);
}
