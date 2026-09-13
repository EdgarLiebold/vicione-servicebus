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

public sealed class JobStateMachineLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2045, 4, 5, 6, 7, 8, TimeSpan.Zero);
    private static readonly Uri ServiceAddress = new("loopback://localhost/job-service");

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-MACHINE", "immediate-submission-snapshots-values-and-requests-capacity")]
    public async Task ImmediateSubmission_SnapshotsValuesAndRequestsCapacityAsync()
    {
        var machine = new JobStateMachine();
        Guid jobId = NewId.NextGuid();
        Guid jobTypeId = NewId.NextGuid();
        var job = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Value"] = "first",
            ["value"] = "last",
        };
        var properties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Region"] = "west",
            ["region"] = "north",
        };
        var saga = new JobSaga { CorrelationId = jobId };
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.JobSubmitted, new JobSubmittedEvent
        {
            JobId = jobId,
            JobTypeId = jobTypeId,
            Timestamp = Now,
            JobTimeout = TimeSpan.FromMinutes(9),
            Job = job,
            JobProperties = properties,
        }, outgoing, sourceAddress: ServiceAddress);

        AssertState(machine, saga, machine.AllocatingJobSlot);
        Assert.Equal(Now, saga.Submitted);
        Assert.Equal(ServiceAddress, saga.ServiceAddress);
        Assert.Equal(TimeSpan.FromMinutes(9), saga.JobTimeout);
        Assert.Equal(jobTypeId, saga.JobTypeId);
        Assert.NotEqual(Guid.Empty, saga.AttemptId);
        Assert.Single(saga.Job);
        Assert.Equal("last", saga.Job["VALUE"]);
        Assert.Single(saga.JobProperties);
        Assert.Equal("north", saga.JobProperties["REGION"]);

        job["value"] = "mutated";
        properties["region"] = "mutated";
        Assert.Equal("last", saga.Job["value"]);
        Assert.Equal("north", saga.JobProperties["region"]);

        AllocateJobSlot request = Assert.Single(outgoing.Messages.OfType<AllocateJobSlot>());
        Assert.Equal(jobId, request.JobId);
        Assert.Equal(jobTypeId, request.JobTypeId);
        Assert.Equal(TimeSpan.FromMinutes(9), request.JobTimeout);
        Assert.Same(saga.JobProperties, request.JobProperties);
        OutgoingMessageRecorder.SendObservation observation = Assert.Single(outgoing.SendObservations);
        Assert.Same(request, observation.Message);
        Assert.Equal(new Uri("loopback://localhost/in-memory-outbox-test"), observation.ResponseAddress);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [RequirementCoverage("REQ-VSB-JOB-STATE-MACHINE", "exhausted-initial-schedule-honors-finalization-policy")]
    public async Task ExhaustedInitialSchedule_HonorsFinalizationPolicyAsync(bool finalizeCompleted, bool expectedFinal)
    {
        var machine = new JobStateMachine();
        var saga = new JobSaga { CorrelationId = NewId.NextGuid() };
        var outgoing = new OutgoingMessageRecorder();
        JobServiceOptions settings = CreateSettings();
        settings.FinalizeCompleted = finalizeCompleted;

        await RaiseAsync(machine, saga, machine.JobSubmitted, new JobSubmittedEvent
        {
            JobId = saga.CorrelationId,
            JobTypeId = NewId.NextGuid(),
            Timestamp = Now,
            JobTimeout = TimeSpan.FromMinutes(1),
            Job = new Dictionary<string, object>(),
            Schedule = new JobScheduleInfo
            {
                CronExpression = "0 15 10 * * ? 2005",
            },
        }, outgoing, settings, sourceAddress: ServiceAddress);

        bool isFinal = machine.Accessor.GetStateExpression(machine.Final).Compile()(saga);
        Assert.Equal(expectedFinal, isFinal);
        if (finalizeCompleted)
            Assert.Empty(outgoing.Messages);
        else
        {
            AssertState(machine, saga, machine.Completed);
            Assert.Equal(JobSlotDisposition.Completed,
                Assert.Single(outgoing.Messages.OfType<JobSlotReleased>()).Disposition);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SCHEDULING-STATE", "date-calculation-covers-one-time-zone-and-end-window-boundaries")]
    public async Task CalculateNextStartDate_CoversOneTimeTimeZoneAndEndWindowBoundariesAsync()
    {
        var settings = CreateSettings();
        var resolvedIdentifiers = new List<string>();
        settings.TimeZoneResolver = id =>
        {
            resolvedIdentifiers.Add(id);
            return TimeZoneInfo.Utc;
        };

        var consumedOneTime = new JobSaga
        {
            StartDate = Now,
            NextStartDate = Now,
        };
        Assert.False(await CalculateNextStartDateAsync(consumedOneTime, settings));
        Assert.Null(consumedOneTime.StartDate);
        Assert.Equal(Now, consumedOneTime.NextStartDate);

        var clearedSchedule = new JobSaga { NextStartDate = Now };
        Assert.False(await CalculateNextStartDateAsync(clearedSchedule, settings));
        Assert.Null(clearedSchedule.NextStartDate);

        var boundedRecurring = new JobSaga
        {
            CronExpression = "0 0 0 ? * *",
            TimeZoneId = "factory-zone",
            EndDate = Now.AddHours(1),
        };
        Assert.False(await CalculateNextStartDateAsync(boundedRecurring, settings));
        Assert.Null(boundedRecurring.NextStartDate);
        Assert.Equal(["factory-zone"], resolvedIdentifiers);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-MACHINE", "scheduled-occurrence-finalizes-old-attempts-and-starts-new-generation")]
    public async Task ScheduledOccurrence_FinalizesOldAttemptsAndStartsANewGenerationAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        Guid priorAttemptId = saga.AttemptId;
        Guid incompleteAttemptId = NewId.NextGuid();
        saga.CronExpression = "0 0 0 ? * *";
        saga.NextStartDate = Now;
        saga.RetryAttempt = 4;
        saga.IncompleteAttempts = [incompleteAttemptId];
        await SetStateAsync(machine, saga, machine.WaitingForSlot);
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.JobSlotWaitElapsed.Received, new JobSlotWaitElapsedEvent
        {
            JobId = saga.CorrelationId,
        }, outgoing);

        AssertState(machine, saga, machine.AllocatingJobSlot);
        Assert.NotEqual(priorAttemptId, saga.AttemptId);
        Assert.Equal(0, saga.RetryAttempt);
        Assert.Null(saga.IncompleteAttempts);
        FinalizeJobAttempt finalize = Assert.Single(outgoing.Messages.OfType<FinalizeJobAttempt>());
        Assert.Equal(saga.CorrelationId, finalize.JobId);
        Assert.Equal(incompleteAttemptId, finalize.AttemptId);
        Assert.Single(outgoing.Messages.OfType<AllocateJobSlot>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SCHEDULING-STATE", "recurring-fault-releases-capacity-and-schedules-next-occurrence")]
    public async Task RecurringFault_ReleasesCapacityAndSchedulesTheNextOccurrenceAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        saga.CronExpression = "0 0 0 ? * *";
        saga.Started = Now.AddMinutes(-2);
        await SetStateAsync(machine, saga, machine.Started);
        var scheduler = new StateMachineTestScheduler(new FixedTimeProvider(Now));
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.AttemptFaulted, new JobAttemptFaultedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = Now,
            Exceptions = new FaultExceptionInfo(new InvalidOperationException("recurring attempt failed")),
        }, outgoing, scheduler: scheduler);

        AssertState(machine, saga, machine.WaitingForSlot);
        Assert.NotNull(saga.NextStartDate);
        Assert.Null(saga.IncompleteAttempts);
        Assert.Equal(JobSlotDisposition.Faulted,
            Assert.Single(outgoing.Messages.OfType<JobSlotReleased>()).Disposition);
        Assert.Single(outgoing.Messages.OfType<FinalizeJobAttempt>());
        StateMachineTestScheduler.ScheduledCall scheduled = Assert.Single(scheduler.Scheduled);
        Assert.Equal(saga.NextStartDate, scheduled.DueAt);
        Assert.IsAssignableFrom<JobSlotWaitElapsed>(scheduled.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-MACHINE", "attempt-start-fault-preserves-and-routes-failure")]
    public async Task AttemptStartFault_PreservesAndRoutesTheFailureAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        saga.Started = Now.AddSeconds(-17);
        await SetStateAsync(machine, saga, machine.StartingJobAttempt);
        var expected = new FaultExceptionInfo(new InvalidOperationException("dispatch failed"));
        var outgoing = new OutgoingMessageRecorder();
        Fault<StartJobAttempt> fault = new FaultEvent<StartJobAttempt>
        {
            FaultId = NewId.NextGuid(),
            Timestamp = Now,
            Message = new StartJobAttemptCommand
            {
                JobId = saga.CorrelationId,
                AttemptId = saga.AttemptId,
                JobTypeId = saga.JobTypeId,
                Job = saga.Job,
                ServiceAddress = saga.ServiceAddress,
                InstanceAddress = new Uri("loopback://localhost/job-instance"),
            },
            Exceptions = [expected],
        };

        await RaiseAsync(machine, saga, machine.StartJobAttemptFaulted, fault, outgoing);

        AssertState(machine, saga, machine.Faulted);
        Assert.Equal(Now, saga.Faulted);
        Assert.Equal(expected.Message, saga.Reason);
        Assert.Equal([saga.AttemptId], saga.IncompleteAttempts);
        FaultJob reported = Assert.Single(outgoing.Messages.OfType<FaultJob>());
        Assert.Same(expected, reported.Exceptions);
        Assert.Equal(TimeSpan.FromSeconds(17), reported.Duration);
        Assert.Single(outgoing.Messages.OfType<JobFaulted>());
        Assert.Equal(JobSlotDisposition.Faulted,
            Assert.Single(outgoing.Messages.OfType<JobSlotReleased>()).Disposition);
        OutgoingMessageRecorder.SendObservation faultObservation = Assert.Single(
            outgoing.SendObservations,
            observation => ReferenceEquals(observation.Message, reported));
        Assert.Equal(saga.CorrelationId, faultObservation.RequestId);
    }

    [Theory]
    [InlineData(CapacityOutcome.Allocated, true)]
    [InlineData(CapacityOutcome.Unavailable, false)]
    [InlineData(CapacityOutcome.Faulted, false)]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "pending-capacity-outcomes-complete-cancellation")]
    public async Task PendingCapacityOutcome_CompletesCancellationAsync(CapacityOutcome outcome, bool expectsRelease)
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        saga.NextStartDate = Now.AddDays(1);
        await SetStateAsync(machine, saga, machine.CancellationPending);
        var outgoing = new OutgoingMessageRecorder();

        switch (outcome)
        {
            case CapacityOutcome.Allocated:
                await RaiseAsync(machine, saga, machine.JobSlotAllocated, new JobSlotAllocatedResponse
                {
                    JobId = saga.CorrelationId,
                    InstanceAddress = new Uri("loopback://localhost/job-instance"),
                }, outgoing);
                break;
            case CapacityOutcome.Unavailable:
                await RaiseAsync(machine, saga, machine.JobSlotUnavailable, new JobSlotUnavailableResponse
                {
                    JobId = saga.CorrelationId,
                }, outgoing);
                break;
            case CapacityOutcome.Faulted:
                await RaiseAsync(machine, saga, machine.AllocateJobSlotFaulted, CreateAllocationFault(saga), outgoing);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        AssertState(machine, saga, machine.Canceled);
        Assert.Null(saga.NextStartDate);
        JobCanceled canceled = Assert.Single(outgoing.Messages.OfType<JobCanceled>());
        Assert.Equal(JobCancellationReasons.CancellationRequested, canceled.Reason);
        Assert.Equal(Now, canceled.Timestamp);
        Assert.Equal(expectsRelease ? 1 : 0, outgoing.Messages.OfType<JobSlotReleased>().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CANCELLATION", "shutdown-cancellation-releases-capacity-and-returns-to-waiting")]
    public async Task ShutdownCancellation_ReleasesCapacityAndReturnsToWaitingAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        await SetStateAsync(machine, saga, machine.Started);
        JobServiceOptions settings = CreateSettings();
        var scheduler = new StateMachineTestScheduler(settings.TimeProvider);
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.AttemptCanceled, new JobAttemptCanceledEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = Now,
            Reason = JobCancellationReasons.Shutdown,
        }, outgoing, settings, scheduler: scheduler);

        AssertState(machine, saga, machine.WaitingForSlot);
        Assert.Equal(JobCancellationReasons.Shutdown, saga.Reason);
        Assert.Equal(JobSlotDisposition.Canceled,
            Assert.Single(outgoing.Messages.OfType<JobSlotReleased>()).Disposition);
        StateMachineTestScheduler.ScheduledCall scheduled = Assert.Single(scheduler.Scheduled);
        Assert.Equal(Now + settings.SlotWaitTime, scheduled.DueAt);
        Assert.IsAssignableFrom<JobSlotWaitElapsed>(scheduled.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-MACHINE", "terminal-late-events-preserve-current-details-and-notifications")]
    public async Task TerminalLateEvents_PreserveCurrentDetailsAndNotificationsAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        await SetStateAsync(machine, saga, machine.Completed);
        DateTimeOffset completedStart = Now.AddMinutes(-3);

        await RaiseAsync(machine, saga, machine.AttemptStarted, new JobAttemptStartedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = completedStart,
            InstanceAddress = new Uri("loopback://localhost/job-instance"),
        }, new OutgoingMessageRecorder());

        var completedMessages = new OutgoingMessageRecorder();
        var checkpoint = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Stage"] = "first",
            ["stage"] = "last",
        };
        await RaiseAsync(machine, saga, machine.AttemptCompleted, new JobAttemptCompletedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = Now,
            Duration = TimeSpan.FromMinutes(3),
            CheckpointChanged = true,
            Checkpoint = checkpoint,
        }, completedMessages);

        AssertState(machine, saga, machine.Completed);
        Assert.Equal(completedStart, saga.Started);
        Dictionary<string, object> storedCheckpoint = Assert.IsType<Dictionary<string, object>>(saga.Checkpoint);
        Assert.Single(storedCheckpoint);
        Assert.Equal("last", storedCheckpoint["STAGE"]);
        checkpoint["stage"] = "mutated";
        Assert.Equal("last", storedCheckpoint["stage"]);
        Assert.Single(completedMessages.Messages.OfType<CompleteJob>());
        Assert.Single(completedMessages.Messages.OfType<JobCompleted>());

        await SetStateAsync(machine, saga, machine.Faulted);
        DateTimeOffset faultedStart = Now.AddMinutes(-2);
        await RaiseAsync(machine, saga, machine.AttemptStarted, new JobAttemptStartedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = faultedStart,
            InstanceAddress = new Uri("loopback://localhost/job-instance"),
        }, new OutgoingMessageRecorder());
        var faultedMessages = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.AttemptFaulted, new JobAttemptFaultedEvent
        {
            JobId = saga.CorrelationId,
            AttemptId = saga.AttemptId,
            Timestamp = Now,
            Exceptions = new FaultExceptionInfo(new InvalidOperationException("late fault")),
            CheckpointChanged = true,
            Checkpoint = new Dictionary<string, object> { ["stage"] = "faulted" },
        }, faultedMessages);

        AssertState(machine, saga, machine.Faulted);
        Assert.Equal(faultedStart, saga.Started);
        Assert.Equal("faulted", saga.Checkpoint!["STAGE"]);
        Assert.Single(faultedMessages.Messages.OfType<FaultJob>());
        Assert.Single(faultedMessages.Messages.OfType<JobFaulted>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FINALIZATION", "terminal-finalization-drains-every-incomplete-attempt")]
    public async Task TerminalFinalization_DrainsEveryIncompleteAttemptAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        Guid first = NewId.NextGuid();
        Guid second = NewId.NextGuid();
        saga.IncompleteAttempts = [first, second];
        await SetStateAsync(machine, saga, machine.Completed);
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(machine, saga, machine.FinalizeJob, new FinalizeJobCommand
        {
            JobId = saga.CorrelationId,
        }, outgoing);

        Assert.True(machine.Accessor.GetStateExpression(machine.Final).Compile()(saga));
        Assert.Null(saga.IncompleteAttempts);
        FinalizeJobAttempt[] finalizations = outgoing.Messages.OfType<FinalizeJobAttempt>().ToArray();
        Assert.Equal([first, second], finalizations.Select(static message => message.AttemptId));
        Assert.All(finalizations, message => Assert.Equal(saga.CorrelationId, message.JobId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-FINALIZATION", "completed-schedule-without-next-occurrence-honors-finalization-policy")]
    public async Task CompletedScheduleWithoutNextOccurrence_HonorsFinalizationPolicyAsync()
    {
        var machine = new JobStateMachine();
        var saga = CreateActiveSaga();
        saga.CronExpression = "0 15 10 * * ? 2005";
        await SetStateAsync(machine, saga, machine.Completed);
        JobServiceOptions settings = CreateSettings();
        settings.FinalizeCompleted = true;

        await RaiseAsync(machine, saga, machine.JobCompleted, new JobCompletedEvent
        {
            JobId = saga.CorrelationId,
            Timestamp = Now,
            Job = saga.Job,
        }, new OutgoingMessageRecorder(), settings);

        Assert.True(machine.Accessor.GetStateExpression(machine.Final).Compile()(saga));
        Assert.Null(saga.NextStartDate);
    }

    private static JobSaga CreateActiveSaga() => new()
    {
        CorrelationId = NewId.NextGuid(),
        AttemptId = NewId.NextGuid(),
        ServiceAddress = ServiceAddress,
        JobTimeout = TimeSpan.FromMinutes(5),
        Job = new Dictionary<string, object> { ["value"] = 42 },
        JobTypeId = NewId.NextGuid(),
        JobProperties = new Dictionary<string, object> { ["region"] = "north" },
    };

    private static JobServiceOptions CreateSettings() => new()
    {
        JobTypeEndpointName = "job-type",
        JobEndpointName = "job",
        JobAttemptEndpointName = "job-attempt",
        JobSagaEndpointAddress = new Uri("loopback://localhost/job-saga"),
        JobTypeSagaEndpointAddress = new Uri("loopback://localhost/job-type-saga"),
        JobAttemptSagaEndpointAddress = new Uri("loopback://localhost/job-attempt-saga"),
        StatusCheckInterval = TimeSpan.FromMinutes(2),
        SlotWaitTime = TimeSpan.FromSeconds(9),
        TimeProvider = new FixedTimeProvider(Now),
    };

    private static Fault<AllocateJobSlot> CreateAllocationFault(JobSaga saga) => new FaultEvent<AllocateJobSlot>
    {
        FaultId = NewId.NextGuid(),
        Timestamp = Now,
        Message = new AllocateJobSlotCommand
        {
            JobId = saga.CorrelationId,
            JobTypeId = saga.JobTypeId,
            JobTimeout = saga.JobTimeout!.Value,
            JobProperties = saga.JobProperties,
        },
        Exceptions = [new FaultExceptionInfo(new InvalidOperationException("capacity failed"))],
    };

    private static async Task<bool> CalculateNextStartDateAsync(JobSaga saga, JobServiceOptions settings)
    {
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StateSetupMessage(),
            sentTime: Now);
        consumeContext.SetTimeProvider(settings.TimeProvider);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, StateSetupMessage>(consumeContext, instance);
        sagaContext.AddOrUpdatePayload<JobSagaSettings>(() => settings, _ => settings);

        return sagaContext.CalculateNextStartDate();
    }

    private static async Task RaiseAsync<T>(
        JobStateMachine machine,
        JobSaga saga,
        Event<T> @event,
        T message,
        OutgoingMessageRecorder outgoing,
        JobServiceOptions? settings = null,
        Uri? sourceAddress = null,
        StateMachineTestScheduler? scheduler = null)
        where T : class
    {
        settings ??= CreateSettings();
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoing,
            sentTime: Now,
            sourceAddress: sourceAddress);
        consumeContext.SetTimeProvider(settings.TimeProvider);
        scheduler?.Attach(consumeContext);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, T>(consumeContext, instance);
        sagaContext.AddOrUpdatePayload<JobSagaSettings>(() => settings, _ => settings);
        BehaviorContext<JobSaga, T> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await ((StateMachine<JobSaga>)machine).RaiseEventAsync(behaviorContext);
    }

    private static async Task SetStateAsync(JobStateMachine machine, JobSaga saga, State state)
    {
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StateSetupMessage(),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<JobSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobSaga, StateSetupMessage>(consumeContext, instance);
        BehaviorContext<JobSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await machine.Accessor.SetAsync(behaviorContext, machine.GetState(state.Name));
    }

    private static void AssertState(JobStateMachine machine, JobSaga saga, State expected) =>
        Assert.True(machine.Accessor.GetStateExpression(expected).Compile()(saga), $"Expected state {expected.Name}.");

    public sealed record StateSetupMessage;

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public enum CapacityOutcome
    {
        Allocated,
        Unavailable,
        Faulted,
    }
}
