using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events.Faults;
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
            Checkpoint = new Dictionary<string, object> { ["checkpoint"] = "retained" },
            JobProperties = new Dictionary<string, object> { ["owner"] = "native" },
        };
        IState expectedState = GetState(machine, sagaState);
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
        await SetStateAsync(machine, saga, machine.WaitingToRetry);

        await Assert.ThrowsAsync<UnhandledEventException>(() =>
            RaiseAsync(machine, saga, machine.AttemptStarted, message));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS", "newer-sequence-replaces-progress")]
    public async Task NewerProgressSequence_ReplacesTheStoredProgressAndSequenceAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateStartedSaga(lastProgressSequenceNumber: 7);
        await SetStateAsync(machine, saga, machine.Started);

        await RaiseAsync(machine, saga, machine.SetJobProgress, new SetJobProgressCommand
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            SequenceNumber = 8,
            Value = 80,
            Limit = 100,
        });

        Assert.Equal(80, saga.LastProgressValue);
        Assert.Equal(100, saga.LastProgressLimit);
        Assert.Equal(8, saga.LastProgressSequenceNumber);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS", "stale-sequence-preserves-progress")]
    public async Task NonIncreasingProgressSequence_PreservesStoredProgressAsync(long sequenceNumber)
    {
        var machine = new JobStateMachine();
        var saga = CreateStartedSaga(lastProgressSequenceNumber: 7);
        await SetStateAsync(machine, saga, machine.Started);

        await RaiseAsync(machine, saga, machine.SetJobProgress, new SetJobProgressCommand
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            SequenceNumber = sequenceNumber,
            Value = 1,
            Limit = 2,
        });

        Assert.Equal(41, saga.LastProgressValue);
        Assert.Equal(100, saga.LastProgressLimit);
        Assert.Equal(7, saga.LastProgressSequenceNumber);
    }

    [Theory]
    [InlineData(TerminalAttemptEvent.Completed, CheckpointUpdate.Preserve)]
    [InlineData(TerminalAttemptEvent.Completed, CheckpointUpdate.Replace)]
    [InlineData(TerminalAttemptEvent.Completed, CheckpointUpdate.Clear)]
    [InlineData(TerminalAttemptEvent.Faulted, CheckpointUpdate.Preserve)]
    [InlineData(TerminalAttemptEvent.Faulted, CheckpointUpdate.Replace)]
    [InlineData(TerminalAttemptEvent.Faulted, CheckpointUpdate.Clear)]
    [InlineData(TerminalAttemptEvent.Canceled, CheckpointUpdate.Preserve)]
    [InlineData(TerminalAttemptEvent.Canceled, CheckpointUpdate.Replace)]
    [InlineData(TerminalAttemptEvent.Canceled, CheckpointUpdate.Clear)]
    [RequirementCoverage("REQ-VSB-JOB-CHECKPOINT", "terminal-attempt-events-atomically-apply-every-checkpoint-update")]
    public async Task TerminalAttemptEvent_AppliesTheCompleteCheckpointUpdateContractAsync(
        TerminalAttemptEvent terminalEvent,
        CheckpointUpdate update)
    {
        var machine = new JobStateMachine();
        var saga = CreateStartedSaga(lastProgressSequenceNumber: 7);
        var original = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["stage"] = "original",
        };
        saga.Checkpoint = original;
        var replacement = new Dictionary<string, object>
        {
            ["Stage"] = "replacement",
        };
        var outgoingMessages = new OutgoingMessageRecorder();
        await SetStateAsync(machine, saga, machine.Started);

        await RaiseTerminalAsync(
            machine,
            saga,
            terminalEvent,
            checkpointChanged: update is not CheckpointUpdate.Preserve,
            checkpoint: update is CheckpointUpdate.Clear ? null : replacement,
            outgoingMessages);

        switch (update)
        {
            case CheckpointUpdate.Preserve:
                Assert.Same(original, saga.Checkpoint);
                Assert.Equal("original", saga.Checkpoint?["STAGE"]);
                break;
            case CheckpointUpdate.Replace:
                Assert.NotSame(replacement, saga.Checkpoint);
                Assert.Equal("replacement", saga.Checkpoint?["stage"]);
                replacement["Stage"] = "mutated after delivery";
                Assert.Equal("replacement", saga.Checkpoint?["stage"]);
                break;
            case CheckpointUpdate.Clear:
                Assert.Null(saga.Checkpoint);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(update), update, null);
        }

        IState expectedState = terminalEvent switch
        {
            TerminalAttemptEvent.Completed => machine.Completed,
            TerminalAttemptEvent.Faulted => machine.Faulted,
            TerminalAttemptEvent.Canceled => machine.Canceled,
            _ => throw new ArgumentOutOfRangeException(nameof(terminalEvent), terminalEvent, null),
        };
        Assert.True(machine.Accessor.GetStateExpression(expectedState).Compile()(saga));
        Assert.NotEmpty(outgoingMessages.Messages);
    }

    [Theory]
    [InlineData(JobSagaState.WaitingToRetry, JobLifecycleStatus.WaitingToRetry)]
    [InlineData(JobSagaState.WaitingForSlot, JobLifecycleStatus.WaitingForSlot)]
    [InlineData(JobSagaState.StartingJobAttempt, JobLifecycleStatus.Starting)]
    [InlineData(JobSagaState.Started, JobLifecycleStatus.Running)]
    [InlineData(JobSagaState.Completed, JobLifecycleStatus.Completed)]
    [InlineData(JobSagaState.Faulted, JobLifecycleStatus.Faulted)]
    [InlineData(JobSagaState.Canceled, JobLifecycleStatus.Canceled)]
    [InlineData(JobSagaState.AllocatingJobSlot, JobLifecycleStatus.AllocatingSlot)]
    [InlineData(JobSagaState.CancellationPending, JobLifecycleStatus.CancellationPending)]
    [RequirementCoverage("REQ-VSB-JOB-STATE", "internal-states-map-to-strongly-typed-lifecycle-status")]
    public void LifecycleStatus_MapsEveryReachablePersistedState(JobSagaState state, JobLifecycleStatus expected)
    {
        var machine = new JobStateMachine();

        Assert.Equal(expected, machine.GetLifecycleStatus(GetState(machine, state)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE", "initial-and-missing-state-mappings-are-explicit")]
    public void LifecycleStatus_MapsInitialAndMissingStatesExplicitly()
    {
        var machine = new JobStateMachine();

        Assert.Equal(JobLifecycleStatus.Submitted, machine.GetLifecycleStatus(machine.Initial));
        Assert.Equal(JobLifecycleStatus.Unknown, machine.GetLifecycleStatus(null));
        Assert.Equal(JobLifecycleStatus.Unknown, machine.GetLifecycleStatus(machine.Final));
    }

    private static JobSaga CreateStartedSaga(long lastProgressSequenceNumber) =>
        new()
        {
            CorrelationId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            ServiceAddress = new Uri("loopback://localhost/job-service"),
            Job = new Dictionary<string, object>(),
            JobTypeId = NewId.NextGuid(),
            LastProgressValue = 41,
            LastProgressLimit = 100,
            LastProgressSequenceNumber = lastProgressSequenceNumber,
        };

    private static IEnumerable<(StaleAttemptEvent Kind, object Message)> CreateStaleEvents(JobSaga saga)
    {
        Guid staleAttemptId = NewId.NextGuid();

        yield return (StaleAttemptEvent.Started, (IJobAttemptStarted)new JobAttemptStartedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc),
            InstanceAddress = new Uri("loopback://localhost/old-instance"),
        });
        yield return (StaleAttemptEvent.Completed, (IJobAttemptCompleted)new JobAttemptCompletedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromMinutes(1),
        });
        yield return (StaleAttemptEvent.Faulted, (IJobAttemptFaulted)new JobAttemptFaultedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
        });
        yield return (StaleAttemptEvent.Canceled, (IJobAttemptCanceled)new JobAttemptCanceledEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
            Reason = "late cancellation from the previous attempt",
        });

        IStartJobAttempt command = new StartJobAttemptCommand
        {
            JobId = saga.CorrelationId,
            AttemptId = staleAttemptId,
            RetryAttempt = 0,
            ServiceAddress = saga.ServiceAddress,
            InstanceAddress = new Uri("loopback://localhost/old-instance"),
            Job = saga.Job,
            JobTypeId = saga.JobTypeId,
        };
        yield return (StaleAttemptEvent.StartFaulted, (Fault<IStartJobAttempt>)new FaultEvent<IStartJobAttempt>
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
            StaleAttemptEvent.Started => RaiseAsync(machine, saga, machine.AttemptStarted, (IJobAttemptStarted)message),
            StaleAttemptEvent.Completed => RaiseAsync(machine, saga, machine.AttemptCompleted, (IJobAttemptCompleted)message),
            StaleAttemptEvent.Faulted => RaiseAsync(machine, saga, machine.AttemptFaulted, (IJobAttemptFaulted)message),
            StaleAttemptEvent.Canceled => RaiseAsync(machine, saga, machine.AttemptCanceled, (IJobAttemptCanceled)message),
            StaleAttemptEvent.StartFaulted => RaiseAsync(machine, saga, machine.StartJobAttemptFaulted, (Fault<IStartJobAttempt>)message),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static Task RaiseTerminalAsync(
        JobStateMachine machine,
        JobSaga saga,
        TerminalAttemptEvent terminalEvent,
        bool checkpointChanged,
        IReadOnlyDictionary<string, object>? checkpoint,
        OutgoingMessageRecorder outgoingMessages) =>
        terminalEvent switch
        {
            TerminalAttemptEvent.Completed => RaiseAsync(machine, saga, machine.AttemptCompleted, (IJobAttemptCompleted)new JobAttemptCompletedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = saga.AttemptId,
                RetryAttempt = saga.RetryAttempt,
                Timestamp = new DateTime(2026, 8, 15, 8, 2, 0, DateTimeKind.Utc),
                Duration = TimeSpan.FromMinutes(1),
                CheckpointChanged = checkpointChanged,
                Checkpoint = checkpoint,
            }, outgoingMessages),
            TerminalAttemptEvent.Faulted => RaiseAsync(machine, saga, machine.AttemptFaulted, (IJobAttemptFaulted)new JobAttemptFaultedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = saga.AttemptId,
                RetryAttempt = saga.RetryAttempt,
                Timestamp = new DateTime(2026, 8, 15, 8, 2, 0, DateTimeKind.Utc),
                Exceptions = new FaultExceptionInfo(new InvalidOperationException("attempt failed")),
                CheckpointChanged = checkpointChanged,
                Checkpoint = checkpoint,
            }, outgoingMessages),
            TerminalAttemptEvent.Canceled => RaiseAsync(machine, saga, machine.AttemptCanceled, (IJobAttemptCanceled)new JobAttemptCanceledEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = saga.AttemptId,
                Timestamp = new DateTime(2026, 8, 15, 8, 2, 0, DateTimeKind.Utc),
                Reason = "cancellation requested",
                CheckpointChanged = checkpointChanged,
                Checkpoint = checkpoint,
            }, outgoingMessages),
            _ => throw new ArgumentOutOfRangeException(nameof(terminalEvent), terminalEvent, null),
        };

    private static async Task RaiseAsync<T>(
        JobStateMachine machine,
        JobSaga saga,
        IEvent<T> @event,
        T message,
        OutgoingMessageRecorder? outgoingMessages = null)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            outgoingMessages: outgoingMessages);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, T>(consumeContext, instance);
        if (outgoingMessages is not null)
        {
            var settings = new JobServiceOptions
            {
                JobAttemptSagaEndpointAddress = new Uri("loopback://localhost/job-attempt-saga"),
                JobSagaEndpointAddress = new Uri("loopback://localhost/job-saga"),
                JobTypeSagaEndpointAddress = new Uri("loopback://localhost/job-type-saga"),
            };
            sagaContext.AddOrUpdatePayload<IJobSagaSettings>(() => settings, _ => settings);
        }
        IBehaviorContext<JobSaga, T> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await ((IStateMachine<JobSaga>)machine).RaiseEventAsync(behaviorContext);
    }

    private static async Task SetStateAsync(JobStateMachine machine, JobSaga saga, IState state)
    {
        var message = new StateSetupMessage();
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, StateSetupMessage>(consumeContext, instance);
        IBehaviorContext<JobSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await machine.Accessor.SetAsync(behaviorContext, machine.GetState(state.Name));
    }

    private static IState GetState(JobStateMachine machine, JobSagaState state) =>
        state switch
        {
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
            saga.Checkpoint,
            saga.JobProperties);

    public enum JobSagaState
    {
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

    public enum TerminalAttemptEvent
    {
        Completed,
        Faulted,
        Canceled,
    }

    public enum CheckpointUpdate
    {
        Preserve,
        Replace,
        Clear,
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
        Dictionary<string, object>? Checkpoint,
        Dictionary<string, object> JobProperties);
}
