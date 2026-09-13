using Microsoft.Extensions.Time.Testing;
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

public sealed class JobAttemptStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2035, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "start-schedules-liveness-and-forwards-the-complete-job")]
    public async Task StartAttempt_SchedulesLivenessAndForwardsTheCompleteJobAsync()
    {
        var machine = new JobAttemptStateMachine();
        Guid attemptId = NewId.NextGuid();
        Guid jobId = NewId.NextGuid();
        Guid jobTypeId = NewId.NextGuid();
        var serviceAddress = new Uri("loopback://localhost/job-service");
        var instanceAddress = new Uri("loopback://localhost/job-instance");
        var job = new Dictionary<string, object> { ["name"] = "invoice-42" };
        var checkpoint = new Dictionary<string, object> { ["offset"] = 17L };
        var properties = new Dictionary<string, object> { ["tenant"] = "north" };
        var command = new StartJobAttemptCommand
        {
            AttemptId = attemptId,
            JobId = jobId,
            JobTypeId = jobTypeId,
            RetryAttempt = 2,
            ServiceAddress = serviceAddress,
            InstanceAddress = instanceAddress,
            Job = job,
            LastProgressValue = 4,
            LastProgressLimit = 10,
            Checkpoint = checkpoint,
            JobProperties = properties,
        };
        var saga = new JobAttemptSaga { CorrelationId = attemptId };
        var outgoing = new OutgoingMessageRecorder();
        var clock = new FakeTimeProvider(Now);
        var scheduler = new StateMachineTestScheduler(clock);
        JobServiceOptions settings = CreateSettings();

        await RaiseAsync(machine, saga, machine.StartJobAttempt, command, settings, scheduler, outgoing, clock);

        AssertState(machine, saga, machine.Starting);
        Assert.Equal(jobId, saga.JobId);
        Assert.Equal(2, saga.RetryAttempt);
        Assert.Equal(serviceAddress, saga.ServiceAddress);
        Assert.Equal(instanceAddress, saga.InstanceAddress);
        StateMachineTestScheduler.ScheduledCall scheduled = Assert.Single(scheduler.Scheduled);
        Assert.Equal(Now + settings.StatusCheckInterval, scheduled.DueAt);
        IJobStatusCheckRequested statusCheck = Assert.IsAssignableFrom<IJobStatusCheckRequested>(scheduled.Message);
        Assert.Equal(attemptId, statusCheck.AttemptId);
        Assert.Equal(jobId, statusCheck.JobId);
        Assert.Equal(scheduled.TokenId, saga.StatusCheckTokenId);

        IStartJob start = Assert.Single(outgoing.Messages.OfType<IStartJob>());
        Assert.Equal(jobId, start.JobId);
        Assert.Equal(attemptId, start.AttemptId);
        Assert.Equal(2, start.RetryAttempt);
        Assert.Equal(jobTypeId, start.JobTypeId);
        Assert.Same(job, start.Job);
        Assert.Equal(4, start.LastProgressValue);
        Assert.Equal(10, start.LastProgressLimit);
        Assert.Same(checkpoint, start.Checkpoint);
        Assert.Same(properties, start.JobProperties);
        OutgoingMessageRecorder.SendObservation startObservation = Assert.Single(outgoing.SendObservations);
        Assert.Same(start, startObservation.Message);
        Assert.Equal(settings.JobAttemptSagaEndpointAddress, startObservation.FaultAddress);

        DateTimeOffset started = Now.AddSeconds(1);
        await RaiseAsync(machine, saga, machine.AttemptStarted, new JobAttemptStartedEvent
        {
            JobId = jobId,
            AttemptId = attemptId,
            RetryAttempt = 2,
            Timestamp = started,
            InstanceAddress = instanceAddress,
        }, settings, scheduler, new OutgoingMessageRecorder(), clock);

        AssertState(machine, saga, machine.Running);
        Assert.Equal(started, saga.Started);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "unanswered-checks-escalate-and-apply-the-retry-delay-boundary")]
    public async Task UnansweredStatusChecks_EscalateAndApplyTheRetryDelayBoundaryAsync(
        int retryAttempt,
        bool expectsRetry)
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(retryAttempt);
        await SetStateAsync(machine, saga, machine.Running);
        var clock = new FakeTimeProvider(Now);
        var scheduler = new StateMachineTestScheduler(clock);
        JobServiceOptions settings = CreateSettings();
        var outgoing = new OutgoingMessageRecorder();
        var statusCheck = new JobStatusCheckRequestedEvent
        {
            AttemptId = saga.CorrelationId,
            JobId = saga.JobId,
        };

        await RaiseAsync(machine, saga, machine.StatusCheckRequested.Received, statusCheck, settings, scheduler, outgoing, clock);
        AssertState(machine, saga, machine.CheckingStatus);
        AssertStatusRequest(outgoing, saga);

        outgoing = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.StatusCheckRequested.Received, statusCheck, settings, scheduler, outgoing, clock);
        AssertState(machine, saga, machine.Suspect);
        AssertStatusRequest(outgoing, saga);

        outgoing = new OutgoingMessageRecorder();
        await RaiseAsync(machine, saga, machine.StatusCheckRequested.Received, statusCheck, settings, scheduler, outgoing, clock);
        AssertState(machine, saga, machine.Faulted);
        IJobAttemptFaulted fault = Assert.Single(outgoing.Messages.OfType<IJobAttemptFaulted>());
        Assert.Equal(saga.JobId, fault.JobId);
        Assert.Equal(saga.CorrelationId, fault.AttemptId);
        Assert.Equal(retryAttempt, fault.RetryAttempt);
        Assert.Equal(Now, fault.Timestamp);
        Assert.Equal(expectsRetry ? settings.SuspectJobRetryDelay : null, fault.RetryDelay);
        Assert.Contains("status check timed out", fault.Exceptions.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(JobAttemptStatusKind.Running, AttemptOutcome.Running)]
    [InlineData(JobAttemptStatusKind.Completed, AttemptOutcome.Finalized)]
    [InlineData(JobAttemptStatusKind.Canceled, AttemptOutcome.Finalized)]
    [InlineData(JobAttemptStatusKind.Faulted, AttemptOutcome.Faulted)]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "every-status-response-selects-its-exact-state")]
    public async Task StatusResponse_SelectsItsExactStateAsync(JobAttemptStatusKind status, AttemptOutcome outcome)
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(0);
        await SetStateAsync(machine, saga, machine.CheckingStatus);
        var clock = new FakeTimeProvider(Now);
        var scheduler = new StateMachineTestScheduler(clock);
        saga.StatusCheckTokenId = NewId.NextGuid();

        await RaiseAsync(machine, saga, machine.AttemptStatus, new JobAttemptStatusResponse
        {
            JobId = saga.JobId,
            AttemptId = saga.CorrelationId,
            Timestamp = Now,
            Status = status,
        }, CreateSettings(), scheduler, new OutgoingMessageRecorder(), clock);

        switch (outcome)
        {
            case AttemptOutcome.Running:
                AssertState(machine, saga, machine.Running);
                Assert.Empty(scheduler.Canceled);
                break;
            case AttemptOutcome.Faulted:
                AssertState(machine, saga, machine.Faulted);
                Assert.Single(scheduler.Canceled);
                break;
            case AttemptOutcome.Finalized:
                Assert.True(machine.Accessor.GetStateExpression(machine.Final).Compile()(saga));
                Assert.Single(scheduler.Canceled);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "start-fault-preserves-exception-and-finalization")]
    public async Task StartFault_PreservesTheFirstExceptionAndCanBeFinalizedAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(2);
        await SetStateAsync(machine, saga, machine.Starting);
        var expected = new FaultExceptionInfo(new InvalidOperationException("dispatch refused"));
        DateTimeOffset faulted = Now.AddMinutes(1);
        Fault<IStartJob> fault = new FaultEvent<IStartJob>
        {
            FaultId = NewId.NextGuid(),
            Timestamp = faulted,
            Message = new StartJobCommand
            {
                JobId = saga.JobId,
                AttemptId = saga.CorrelationId,
                JobTypeId = NewId.NextGuid(),
                Job = new Dictionary<string, object>(),
            },
            Exceptions = [expected],
        };
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(
            machine,
            saga,
            machine.StartJobFaulted,
            fault,
            CreateSettings(),
            new StateMachineTestScheduler(new FakeTimeProvider(Now)),
            outgoing,
            new FakeTimeProvider(Now));

        AssertState(machine, saga, machine.Faulted);
        Assert.Equal(faulted, saga.Faulted);
        IJobAttemptFaulted reported = Assert.Single(outgoing.Messages.OfType<IJobAttemptFaulted>());
        Assert.Same(expected, reported.Exceptions);

        await RaiseAsync(
            machine,
            saga,
            machine.FinalizeJobAttempt,
            new FinalizeJobAttemptCommand { AttemptId = saga.CorrelationId },
            CreateSettings(),
            new StateMachineTestScheduler(new FakeTimeProvider(Now)),
            new OutgoingMessageRecorder(),
            new FakeTimeProvider(Now));

        Assert.True(machine.Accessor.GetStateExpression(machine.Final).Compile()(saga));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "cancel-is-forwarded-and-terminal-completion-finalizes")]
    public async Task CancellationCommand_IsForwardedAndCompletionFinalizesAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(0);
        await SetStateAsync(machine, saga, machine.Running);
        var cancel = new CancelJobAttemptCommand
        {
            JobId = saga.JobId,
            AttemptId = saga.CorrelationId,
            Reason = "shutdown",
        };
        var scheduler = new StateMachineTestScheduler(new FakeTimeProvider(Now));
        var outgoing = new OutgoingMessageRecorder();

        await RaiseAsync(
            machine,
            saga,
            machine.CancelJobAttempt,
            cancel,
            CreateSettings(),
            scheduler,
            outgoing,
            new FakeTimeProvider(Now));

        Assert.Same(cancel, Assert.Single(outgoing.Messages.OfType<ICancelJobAttempt>()));
        OutgoingMessageRecorder.SendObservation cancelObservation = Assert.Single(outgoing.SendObservations);
        Assert.Equal(saga.CorrelationId, cancelObservation.RequestId);
        Assert.Equal(CreateSettings().JobAttemptSagaEndpointAddress, cancelObservation.ResponseAddress);
        AssertState(machine, saga, machine.Running);

        saga.StatusCheckTokenId = NewId.NextGuid();
        await RaiseAsync(machine, saga, machine.AttemptCompleted, new JobAttemptCompletedEvent
        {
            JobId = saga.JobId,
            AttemptId = saga.CorrelationId,
            RetryAttempt = saga.RetryAttempt,
            Timestamp = Now,
            Duration = TimeSpan.FromMinutes(2),
        }, CreateSettings(), scheduler, new OutgoingMessageRecorder(), new FakeTimeProvider(Now));

        Assert.True(machine.Accessor.GetStateExpression(machine.Final).Compile()(saga));
        Assert.Single(scheduler.Canceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "start-fault-requires-exception-details")]
    public async Task StartFault_RejectsMissingExceptionDetailsAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(0);
        await SetStateAsync(machine, saga, machine.Starting);
        Fault<IStartJob> fault = new FaultEvent<IStartJob>
        {
            FaultId = NewId.NextGuid(),
            Timestamp = Now,
            Message = new StartJobCommand
            {
                JobId = saga.JobId,
                AttemptId = saga.CorrelationId,
                JobTypeId = NewId.NextGuid(),
                Job = new Dictionary<string, object>(),
            },
            Exceptions = [],
        };

        EventExecutionException exception = await Assert.ThrowsAsync<EventExecutionException>(() => RaiseAsync(
            machine,
            saga,
            machine.StartJobFaulted,
            fault,
            CreateSettings(),
            new StateMachineTestScheduler(new FakeTimeProvider(Now)),
            new OutgoingMessageRecorder(),
            new FakeTimeProvider(Now)));

        InvalidOperationException cause = Assert.IsType<InvalidOperationException>(exception.GetBaseException());
        Assert.Contains("must include exception details", cause.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "reported-attempt-fault-records-time-and-stops-liveness")]
    public async Task ReportedAttemptFault_RecordsTimeAndStopsLivenessAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(1);
        await SetStateAsync(machine, saga, machine.Running);
        saga.StatusCheckTokenId = NewId.NextGuid();
        DateTimeOffset faulted = Now.AddMinutes(4);
        var scheduler = new StateMachineTestScheduler(new FakeTimeProvider(Now));

        await RaiseAsync(machine, saga, machine.AttemptFaulted, new JobAttemptFaultedEvent
        {
            JobId = saga.JobId,
            AttemptId = saga.CorrelationId,
            RetryAttempt = saga.RetryAttempt,
            Timestamp = faulted,
            Exceptions = new FaultExceptionInfo(new InvalidOperationException("consumer failed")),
        }, CreateSettings(), scheduler, new OutgoingMessageRecorder(), new FakeTimeProvider(Now));

        AssertState(machine, saga, machine.Faulted);
        Assert.Equal(faulted, saga.Faulted);
        Assert.Single(scheduler.Canceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "late-start-acknowledgement-enriches-but-does-not-reopen-faulted-attempt")]
    public async Task LateStartAcknowledgement_EnrichesButDoesNotReopenAFaultedAttemptAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(0);
        Uri retainedInstance = saga.InstanceAddress;
        await SetStateAsync(machine, saga, machine.Faulted);
        DateTimeOffset started = Now.AddMinutes(5);

        await RaiseAsync(machine, saga, machine.AttemptStarted, new JobAttemptStartedEvent
        {
            JobId = saga.JobId,
            AttemptId = saga.CorrelationId,
            RetryAttempt = 4,
            Timestamp = started,
            InstanceAddress = new Uri("loopback://localhost/late-instance"),
        }, CreateSettings(), new StateMachineTestScheduler(new FakeTimeProvider(Now)), new OutgoingMessageRecorder(), new FakeTimeProvider(Now));

        AssertState(machine, saga, machine.Faulted);
        Assert.Equal(started, saga.Started);
        Assert.Equal(4, saga.RetryAttempt);
        Assert.Equal(retainedInstance, saga.InstanceAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ATTEMPT-SUPERVISION", "starting-timeout-reports-instance-and-retry-policy")]
    public async Task StartingTimeout_ReportsTheAssignedInstanceAndRetryPolicyAsync()
    {
        var machine = new JobAttemptStateMachine();
        var saga = CreateActiveSaga(1);
        await SetStateAsync(machine, saga, machine.Starting);
        var outgoing = new OutgoingMessageRecorder();
        JobServiceOptions settings = CreateSettings();

        await RaiseAsync(machine, saga, machine.StatusCheckRequested.Received, new JobStatusCheckRequestedEvent
        {
            AttemptId = saga.CorrelationId,
            JobId = saga.JobId,
        }, settings, new StateMachineTestScheduler(new FakeTimeProvider(Now)), outgoing, new FakeTimeProvider(Now));

        AssertState(machine, saga, machine.Faulted);
        IJobAttemptFaulted fault = Assert.Single(outgoing.Messages.OfType<IJobAttemptFaulted>());
        Assert.Equal(saga.JobId, fault.JobId);
        Assert.Equal(saga.CorrelationId, fault.AttemptId);
        Assert.Equal(1, fault.RetryAttempt);
        Assert.Equal(Now, fault.Timestamp);
        Assert.Equal(settings.SuspectJobRetryDelay, fault.RetryDelay);
        Assert.Contains(saga.InstanceAddress.ToString(), fault.Exceptions.Message, StringComparison.Ordinal);
        Assert.Contains("Suspect", fault.Exceptions.Message, StringComparison.Ordinal);
    }

    private static JobAttemptSaga CreateActiveSaga(int retryAttempt) => new()
    {
        CorrelationId = NewId.NextGuid(),
        JobId = NewId.NextGuid(),
        RetryAttempt = retryAttempt,
        ServiceAddress = new Uri("loopback://localhost/job-service"),
        InstanceAddress = new Uri("loopback://localhost/job-instance"),
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
        SuspectJobRetryCount = 3,
        SuspectJobRetryDelay = TimeSpan.FromSeconds(13),
    };

    private static void AssertStatusRequest(OutgoingMessageRecorder outgoing, JobAttemptSaga saga)
    {
        IGetJobAttemptStatus request = Assert.Single(outgoing.Messages.OfType<IGetJobAttemptStatus>());
        Assert.Equal(saga.JobId, request.JobId);
        Assert.Equal(saga.CorrelationId, request.AttemptId);
        OutgoingMessageRecorder.SendObservation observation = Assert.Single(outgoing.SendObservations);
        Assert.Equal(saga.CorrelationId, observation.RequestId);
        Assert.Equal(CreateSettings().JobAttemptSagaEndpointAddress, observation.ResponseAddress);
    }

    private static void AssertState(JobAttemptStateMachine machine, JobAttemptSaga saga, IState expected) =>
        Assert.True(machine.Accessor.GetStateExpression(expected).Compile()(saga), $"Expected state {expected.Name}.");

    private static async Task RaiseAsync<T>(
        JobAttemptStateMachine machine,
        JobAttemptSaga saga,
        IEvent<T> @event,
        T message,
        JobServiceOptions settings,
        StateMachineTestScheduler scheduler,
        OutgoingMessageRecorder outgoing,
        TimeProvider timeProvider)
        where T : class
    {
        ConsumeContext<T> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoing,
            sentTime: timeProvider.GetUtcNow());
        consumeContext.SetTimeProvider(timeProvider);
        scheduler.Attach(consumeContext);
        var instance = new SagaInstance<JobAttemptSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobAttemptSaga, T>(consumeContext, instance);
        sagaContext.AddOrUpdatePayload<IJobSagaSettings>(() => settings, _ => settings);
        IBehaviorContext<JobAttemptSaga, T> behaviorContext =
            new ViciOneServiceBusStateMachine<JobAttemptSaga>.BehaviorContextProxy<T>(machine, sagaContext, sagaContext, @event);

        await ((IStateMachine<JobAttemptSaga>)machine).RaiseEventAsync(behaviorContext);
    }

    private static async Task SetStateAsync(JobAttemptStateMachine machine, JobAttemptSaga saga, IState state)
    {
        ConsumeContext<StateSetupMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new StateSetupMessage(),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<JobAttemptSaga>(saga);
        await instance.MarkInUseAsync(consumeContext.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<JobAttemptSaga, StateSetupMessage>(consumeContext, instance);
        IBehaviorContext<JobAttemptSaga> behaviorContext =
            new ViciOneServiceBusStateMachine<JobAttemptSaga>.BehaviorContextProxy(machine, sagaContext, machine.Initial.Enter);

        await machine.Accessor.SetAsync(behaviorContext, machine.GetState(state.Name));
    }

    public sealed record StateSetupMessage;

    public enum AttemptOutcome
    {
        Running,
        Faulted,
        Finalized,
    }
}
