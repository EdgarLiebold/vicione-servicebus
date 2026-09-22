using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.JobService.Scheduling;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coordinates one job across submission, scheduling, retries, execution, and finalization.</summary>
internal sealed class JobStateMachine :
    ViciOneServiceBusStateMachine<JobSaga>
{
    /// <summary>Defines job submission, scheduling, execution, retry, cancellation, and finalization behavior.</summary>
    public JobStateMachine()
    {
        ConfigureEventsAndSchedules();
        ConfigureSubmissionAndSlotAllocation();
        ConfigureAttemptLifecycle();
        ConfigureTerminalAttemptEvents();
        ConfigureStaleAttemptsAndUpdates();
        ConfigureStateQuery();
        ConfigureCancellation();
        ConfigureRetries();
        ConfigureManualExecutionAndFinalization();
        ConfigureRecurringSubmissions();
        ConfigureStateEntryActivities();
    }

    void ConfigureEventsAndSchedules()
    {
        Event(() => JobSubmitted, x => x.CorrelateById(m => m.Message.JobId));

        Event(() => JobSlotAllocated, x =>
        {
            x.ConfigureConsumeTopology = false;
        });
        Event(() => JobSlotUnavailable, x =>
        {
            x.ConfigureConsumeTopology = false;
        });
        Event(() => AllocateJobSlotFaulted, x =>
        {
            x.ConfigureConsumeTopology = false;
        });

        Event(() => StartJobAttemptFaulted, x =>
        {
            x.ConfigureConsumeTopology = false;
        });

        Event(() => GetJobState, x =>
        {
            x.ReadOnly = true;
            x.OnMissingInstance(i => i.ExecuteAwaited(context => context.RespondAsync<IJobState>(new JobStateResponse
            {
                JobId = context.Message.JobId,
                Status = JobLifecycleStatus.NotFound
            })));
        });

        Schedule(() => JobSlotWaitElapsed, instance => instance.JobSlotWaitToken, x =>
        {
            x.DelayProvider = context => (context.GetPayload<IJobSagaSettings>()
                ?? throw new InvalidOperationException("The job saga settings payload is required.")).SlotWaitTime;
            x.Received = r =>
            {
                r.CorrelateById(context => context.Message.JobId);
                r.ConfigureConsumeTopology = false;
            };
        });

        Schedule(() => JobRetryDelayElapsed, instance => instance.JobRetryDelayToken, x =>
        {
            x.Received = r =>
            {
                r.CorrelateById(context => context.Message.JobId);
                r.ConfigureConsumeTopology = false;
            };
        });

        InstanceState(x => x.CurrentState, WaitingForSlot, Started, Completed, Faulted, Canceled, StartingJobAttempt,
            AllocatingJobSlot, WaitingToRetry, CancellationPending);
    }

    void ConfigureSubmissionAndSlotAllocation()
    {
        Initially(
            When(JobSubmitted)
                .InitializeJob()
                .IfElse(context => context.IsScheduledJob(),
                    scheduled => scheduled
                        .IfElse(context => context.CalculateNextStartDate(),
                            start => start
                                .WaitForNextScheduledTime(this),
                            noStart => noStart
                                .IfElse(context => (context.GetPayload<IJobSagaSettings>()
                                        ?? throw new InvalidOperationException("The job saga settings payload is required.")).FinalizeCompleted,
                                    final => final.Finalize(),
                                    complete => complete.TransitionTo(Completed)
                                )
                        ),
                    immediate => immediate
                        .RequestJobSlot(this)
                )
        );

        During(AllocatingJobSlot,
            When(JobSlotAllocated)
                .RequestStartJob(this),
            When(JobSlotUnavailable)
                .WaitForJobSlot(this),
            When(AllocateJobSlotFaulted)
                .WaitForJobSlot(this),
            Ignore(AttemptStarted),
            Ignore(AttemptCompleted),
            Ignore(AttemptCanceled)
        );

        During(WaitingForSlot,
            When(JobSlotWaitElapsed.Received)
                .If(context => context.IsScheduledJob() && context.Saga.NextStartDate.HasValue,
                    scheduled => scheduled
                        .FinalizeJobAttempts()
                        .Then(context =>
                        {
                            context.Saga.AttemptId = NewId.NextGuid();
                            context.Saga.RetryAttempt = 0;
                        })
                )
                .RequestJobSlot(this),
            Ignore(AttemptStarted),
            Ignore(AttemptCompleted),
            Ignore(AttemptCanceled)
        );
    }

    void ConfigureAttemptLifecycle()
    {
        During(StartingJobAttempt,
            When(StartJobAttemptFaulted, context => context.Saga.AttemptId == context.Message.Message.AttemptId)
                .Then(context =>
                {
                    context.AddIncompleteAttempt(context.Message.Message.AttemptId);
                    context.Saga.Faulted = context.Message.Timestamp;
                    context.Saga.Reason = context.Message.Exceptions.FirstOrDefault()?.Message;
                })
                .NotifyJobFaulted()
                .TransitionTo(Faulted),
            Ignore(JobSlotAllocated),
            Ignore(JobSlotUnavailable)
        );

        During(Started, Completed, Faulted,
            Ignore(StartJobAttemptFaulted),
            Ignore(JobSlotAllocated),
            Ignore(JobSlotUnavailable)
        );

        During(StartingJobAttempt, Started,
            When(AttemptStarted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => context.Saga.Started = context.Message.Timestamp)
                .TransitionTo(Started));

        During(StartingJobAttempt, Started,
            When(AttemptCompleted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context =>
                {
                    context.Saga.Completed = context.Message.Timestamp;
                    context.Saga.Duration = context.Message.Duration;
                    ApplyCheckpointUpdate(context.Saga, context.Message.CheckpointChanged, context.Message.Checkpoint);
                })
                .NotifyJobCompleted()
                .TransitionTo(Completed));

        During(StartingJobAttempt, Started,
            When(AttemptFaulted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context =>
                {
                    context.AddIncompleteAttempt(context.Message.AttemptId);

                    context.Saga.Faulted = context.Message.Timestamp;
                    context.Saga.Reason = context.Message.Exceptions?.Message ?? "Job Attempt Faulted (unknown reason)";
                    ApplyCheckpointUpdate(context.Saga, context.Message.CheckpointChanged, context.Message.Checkpoint);
                })
                .IfElse(context => context.Message.RetryDelay.HasValue,
                    retry => retry
                        .Schedule(JobRetryDelayElapsed, context => new JobRetryDelayElapsedEvent { JobId = context.Message.JobId },
                            context => context.Message.RetryDelay
                                ?? throw new InvalidOperationException("A retry delay is required to schedule the retry event."))
                        .TransitionTo(WaitingToRetry),
                    fault => fault
                        .NotifyJobFaulted()
                        .IfElse(context => context.IsScheduledJob(),
                            scheduled => scheduled
                                .DetermineNextStartDate()
                                .IfElse(context => context.Saga.NextStartDate.HasValue,
                                    start => start
                                        .SendJobSlotReleased(JobSlotDisposition.Faulted)
                                        .FinalizeJobAttempts()
                                        .WaitForNextScheduledTime(this),
                                    noStart => noStart
                                        .TransitionTo(Faulted)
                                ),
                            notScheduled => notScheduled
                                .TransitionTo(Faulted)
                        )
                )
        );
    }

    void ConfigureTerminalAttemptEvents()
    {
        During(Completed,
            When(AttemptCompleted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => ApplyCheckpointUpdate(context.Saga, context.Message.CheckpointChanged, context.Message.Checkpoint))
                .FinalizeJobAttempts()
                .NotifyJobCompleted(),
            When(AttemptStarted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => context.Saga.Started = context.Message.Timestamp),
            When(JobCompleted)
                .IfElse(context => context.IsScheduledJob(),
                    scheduled => scheduled
                        .DetermineNextStartDate()
                        .IfElse(context => context.Saga.NextStartDate.HasValue,
                            start => start
                                .WaitForNextScheduledTime(this),
                            noStart => noStart
                                .If(context => (context.GetPayload<IJobSagaSettings>()
                                        ?? throw new InvalidOperationException("The job saga settings payload is required.")).FinalizeCompleted,
                                    x => x.Finalize()
                                )
                        ),
                    notScheduled => notScheduled
                        .If(context => (context.GetPayload<IJobSagaSettings>()
                                ?? throw new InvalidOperationException("The job saga settings payload is required.")).FinalizeCompleted,
                            x => x.Finalize())
                )
        );

        During(Faulted,
            When(AttemptFaulted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => ApplyCheckpointUpdate(context.Saga, context.Message.CheckpointChanged, context.Message.Checkpoint))
                .NotifyJobFaulted(),
            When(AttemptStarted, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => context.Saga.Started = context.Message.Timestamp));


        During(StartingJobAttempt, Started,
            When(AttemptCanceled, context => context.Saga.AttemptId == context.Message.AttemptId)
                .Then(context => ApplyCheckpointUpdate(context.Saga, context.Message.CheckpointChanged, context.Message.Checkpoint))
                .IfElse(context => string.Equals(context.Message.Reason, JobCancellationReasons.Shutdown, StringComparison.Ordinal),
                    shutdown => shutdown
                        .Then(context => context.Saga.Reason = context.Message.GetCancellationReason())
                        .SendJobSlotReleased(JobSlotDisposition.Canceled)
                        .WaitForJobSlot(this),
                    other => other
                        .PublishJobCanceled(context => context.Message.GetCancellationReason())
                        .SendJobSlotReleased(JobSlotDisposition.Canceled)
                        .ClearNextStartDate()
                        .TransitionTo(Canceled)
                )
        );
    }

    void ConfigureStaleAttemptsAndUpdates()
    {
        // AttemptId is the generation token for a job. Messages from an earlier generation are valid late deliveries,
        // not errors, and must never mutate the current saga. Current-generation events retain the existing state rules.
        During([WaitingToRetry, Canceled, CancellationPending],
            Ignore(AttemptStarted, context => context.Saga.AttemptId != context.Message.AttemptId));

        During([WaitingToRetry, Faulted, Canceled, CancellationPending],
            Ignore(AttemptCompleted, context => context.Saga.AttemptId != context.Message.AttemptId));

        During([WaitingForSlot, Completed, Canceled, AllocatingJobSlot, CancellationPending],
            Ignore(AttemptFaulted, context => context.Saga.AttemptId != context.Message.AttemptId));

        During([WaitingToRetry, Completed, Faulted, CancellationPending],
            Ignore(AttemptCanceled, context => context.Saga.AttemptId != context.Message.AttemptId));

        During([WaitingForSlot, WaitingToRetry, Canceled, AllocatingJobSlot, CancellationPending],
            Ignore(StartJobAttemptFaulted, context => context.Saga.AttemptId != context.Message.Message.AttemptId));

        During([StartingJobAttempt, Started, Completed, Faulted, Canceled, WaitingToRetry],
            When(SetJobProgress)
                .Then(context =>
                {
                    if (context.Saga.AttemptId == context.Message.AttemptId
                        && context.Message.SequenceNumber > (context.Saga.LastProgressSequenceNumber ?? 0))
                    {
                        context.Saga.LastProgressValue = context.Message.Value;
                        context.Saga.LastProgressLimit = context.Message.Limit;
                        context.Saga.LastProgressSequenceNumber = context.Message.SequenceNumber;
                    }
                }));

        During([Started, Completed, Faulted, Canceled, WaitingToRetry],
            When(SaveJobCheckpoint)
                .Then(context =>
                {
                    if (context.Saga.AttemptId == context.Message.AttemptId)
                        ReplaceCheckpoint(context.Saga, context.Message.Checkpoint);
                }));
    }

    void ConfigureStateQuery()
    {
        DuringAny(
            When(GetJobState)
                .RespondAwaited(async context =>
                {
                    IState? state = await Accessor.GetAsync(context).ConfigureAwait(false);
                    return new JobStateResponse
                    {
                        JobId = context.Message.JobId,
                        Submitted = context.Saga.Submitted,
                        Started = context.Saga.Started,
                        Completed = context.Saga.Completed,
                        Duration = context.Saga.Duration,
                        Faulted = context.Saga.Faulted,
                        Reason = context.Saga.Reason,
                        LastRetryAttempt = context.Saga.RetryAttempt,
                        Status = GetLifecycleStatus(state),
                        ProgressValue = context.Saga.LastProgressValue,
                        ProgressLimit = context.Saga.LastProgressLimit,
                        Checkpoint = context.Saga.Checkpoint,
                        NextStartDate = context.Saga.NextStartDate?.UtcDateTime,
                        IsRecurring = !string.IsNullOrWhiteSpace(context.Saga.CronExpression),
                        StartDate = context.Saga.StartDate?.UtcDateTime,
                        EndDate = context.Saga.EndDate?.UtcDateTime
                    };
                })
        );
    }

    void ConfigureCancellation()
    {
        During([WaitingForSlot, WaitingToRetry],
            When(CancelJob)
                .Unschedule(JobSlotWaitElapsed)
                .ClearNextStartDate()
                .PublishJobCanceled(context => context.Message.GetCancellationReason())
                .TransitionTo(Canceled)
        );

        During(Canceled,
            Ignore(CancelJob),
            Ignore(AttemptCanceled));

        During(Completed, Faulted,
            Ignore(CancelJob));

        During([StartingJobAttempt, Started],
            When(CancelJob)
                .CancelCurrentJobAttempt());

        During(AllocatingJobSlot,
            When(CancelJob)
                .Then(context => context.Saga.Reason = context.Message.GetCancellationReason())
                .TransitionTo(CancellationPending));

        During(CancellationPending,
            When(JobSlotAllocated)
                .ClearNextStartDate()
                .PublishJobCanceled(context => context.Saga.Reason ?? JobCancellationReasons.CancellationRequested)
                .SendJobSlotReleased(JobSlotDisposition.Canceled)
                .TransitionTo(Canceled),
            When(JobSlotUnavailable)
                .ClearNextStartDate()
                .PublishJobCanceled(context => context.Saga.Reason ?? JobCancellationReasons.CancellationRequested)
                .TransitionTo(Canceled),
            When(AllocateJobSlotFaulted)
                .ClearNextStartDate()
                .PublishJobCanceled(context => context.Saga.Reason ?? JobCancellationReasons.CancellationRequested)
                .TransitionTo(Canceled)
        );
    }

    void ConfigureRetries()
    {
        During([AllocatingJobSlot, StartingJobAttempt, Started, Completed, CancellationPending],
            Ignore(RetryJob));

        During(WaitingForSlot,
            When(RetryJob)
                .Unschedule(JobSlotWaitElapsed));

        During(WaitingToRetry,
            When(RetryJob)
                .Unschedule(JobRetryDelayElapsed));

        During(WaitingForSlot, WaitingToRetry, Faulted, Canceled,
            When(RetryJob)
                .RequestRetryJobSlot(this));

        During(WaitingToRetry,
            Ignore(AttemptFaulted),
            When(JobRetryDelayElapsed.Received)
                .RequestRetryJobSlot(this));
    }

    void ConfigureManualExecutionAndFinalization()
    {
        // Explicit execution is valid only while the saga is waiting for capacity or its scheduled event.
        During([AllocatingJobSlot, StartingJobAttempt, Started, Completed, Canceled, Faulted, WaitingToRetry, CancellationPending],
            Ignore(RunJob));

        During(WaitingForSlot,
            When(RunJob)
                .Unschedule(JobSlotWaitElapsed)
                .RequestJobSlot(this));


        // Terminal sagas finalize only after the scheduled-event wait has been released.
        During([WaitingForSlot, AllocatingJobSlot, StartingJobAttempt, Started, WaitingToRetry],
            Ignore(FinalizeJob));

        During(Canceled, Faulted, Completed,
            When(FinalizeJob)
                .FinalizeJobAttempts()
                .Finalize());
    }

    void ConfigureRecurringSubmissions()
    {
        // A repeated submission updates a recurring schedule; other duplicates leave saga state unchanged.
        DuringAny(
            When(JobSubmitted)
                .IfElse(context => context.IsScheduledJob(), x => x.UpdateRecurringJob(),
                    x => x.Then(context => LogContext.Warning?.Log("Duplicate Job Submission: {JobTypeId} {JobId}", context.Message.JobTypeId,
                        context.Message.JobId)))
        );

        // Waiting and terminal sagas accept the next occurrence of a recurring schedule.
        During([WaitingForSlot, Canceled, Completed, Faulted],
            When(JobSubmitted)
                .If(context => context.IsScheduledJob() && context.CalculateNextStartDate(),
                    start => start
                        .FinalizeJobAttempts()
                        .WaitForNextScheduledTime(this)
                )
        );
    }

    void ConfigureStateEntryActivities()
    {
        WhenEnter(Completed, x => x.SendJobSlotReleased(JobSlotDisposition.Completed));
        WhenEnter(Faulted, x => x.SendJobSlotReleased(JobSlotDisposition.Faulted));
        WhenEnter(WaitingToRetry, x => x.SendJobSlotReleased(JobSlotDisposition.Faulted));

        SetCompletedWhenFinalized();
    }

    static void ApplyCheckpointUpdate(
        JobSaga saga,
        bool checkpointChanged,
        IReadOnlyDictionary<string, object>? checkpoint)
    {
        if (checkpointChanged)
            ReplaceCheckpoint(saga, checkpoint);
    }

    static void ReplaceCheckpoint(JobSaga saga, IReadOnlyDictionary<string, object>? checkpoint)
    {
        saga.Checkpoint = checkpoint is null ? null : JobPropertySnapshot.Create(checkpoint);
    }

    /// <summary>Maps an internal state-machine state to the stable lifecycle contract returned to callers.</summary>
    /// <param name="state">The current state-machine state.</param>
    /// <returns>The corresponding public lifecycle status.</returns>
    internal JobLifecycleStatus GetLifecycleStatus(IState? state)
    {
        if (state == null)
            return JobLifecycleStatus.Unknown;
        if (state == Initial)
            return JobLifecycleStatus.Submitted;
        if (state == WaitingForSlot)
            return JobLifecycleStatus.WaitingForSlot;
        if (state == AllocatingJobSlot)
            return JobLifecycleStatus.AllocatingSlot;
        if (state == StartingJobAttempt)
            return JobLifecycleStatus.Starting;
        if (state == Started)
            return JobLifecycleStatus.Running;
        if (state == WaitingToRetry)
            return JobLifecycleStatus.WaitingToRetry;
        if (state == Completed)
            return JobLifecycleStatus.Completed;
        if (state == Faulted)
            return JobLifecycleStatus.Faulted;
        if (state == Canceled)
            return JobLifecycleStatus.Canceled;
        if (state == CancellationPending)
            return JobLifecycleStatus.CancellationPending;

        return JobLifecycleStatus.Unknown;
    }

    /// <summary>Gets the state in which the job is waiting for a configured retry delay.</summary>
    public IState WaitingToRetry { get; } = null!;
    /// <summary>Gets the state in which the job is waiting for capacity or its scheduled start.</summary>
    public IState WaitingForSlot { get; } = null!;
    /// <summary>Gets the state in which a consumer is executing the current attempt.</summary>
    public IState Started { get; } = null!;
    /// <summary>Gets the state in which the most recent execution completed successfully.</summary>
    public IState Completed { get; } = null!;
    /// <summary>Gets the state in which cancellation ended the job.</summary>
    public IState Canceled { get; } = null!;
    /// <summary>Gets the state in which an unrecoverable execution failure ended the job.</summary>
    public IState Faulted { get; } = null!;
    /// <summary>Gets the state in which a capacity request is outstanding.</summary>
    public IState AllocatingJobSlot { get; } = null!;
    /// <summary>Gets the state in which a slot is allocated and local execution is starting.</summary>
    public IState StartingJobAttempt { get; } = null!;
    /// <summary>Gets the state in which cancellation waits for an outstanding capacity request.</summary>
    public IState CancellationPending { get; } = null!;

    /// <summary>Gets the response that identifies the service instance assigned to the job.</summary>
    public IEvent<IJobSlotAllocated> JobSlotAllocated { get; } = null!;
    /// <summary>Gets the response indicating that no service instance currently has capacity.</summary>
    public IEvent<IJobSlotUnavailable> JobSlotUnavailable { get; } = null!;
    /// <summary>Gets the fault emitted when capacity allocation fails.</summary>
    public IEvent<Fault<IAllocateJobSlot>> AllocateJobSlotFaulted { get; } = null!;
    /// <summary>Gets the fault emitted when attempt coordination cannot start.</summary>
    public IEvent<Fault<IStartJobAttempt>> StartJobAttemptFaulted { get; } = null!;
    /// <summary>Gets a new or updated job submission.</summary>
    public IEvent<IJobSubmitted> JobSubmitted { get; } = null!;
    /// <summary>Gets the notification that the current attempt started.</summary>
    public IEvent<IJobAttemptStarted> AttemptStarted { get; } = null!;
    /// <summary>Gets the notification that the current attempt completed successfully.</summary>
    public IEvent<IJobAttemptCompleted> AttemptCompleted { get; } = null!;
    /// <summary>Gets the notification that the current attempt was canceled.</summary>
    public IEvent<IJobAttemptCanceled> AttemptCanceled { get; } = null!;
    /// <summary>Gets the notification that the current attempt faulted.</summary>
    public IEvent<IJobAttemptFaulted> AttemptFaulted { get; } = null!;
    /// <summary>Gets the local-runtime acknowledgement that terminal completion was delivered.</summary>
    public IEvent<IJobCompleted> JobCompleted { get; } = null!;
    /// <summary>Gets a request to cancel the job.</summary>
    public IEvent<ICancelJob> CancelJob { get; } = null!;
    /// <summary>Gets a request to retry the job immediately.</summary>
    public IEvent<IRetryJob> RetryJob { get; } = null!;
    /// <summary>Gets a request to run a scheduled job immediately.</summary>
    public IEvent<IRunJob> RunJob { get; } = null!;
    /// <summary>Gets a request to remove terminal job state.</summary>
    public IEvent<IFinalizeJob> FinalizeJob { get; } = null!;
    /// <summary>Gets a sequenced progress update from the current attempt.</summary>
    public IEvent<ISetJobProgress> SetJobProgress { get; } = null!;
    /// <summary>Gets a durable application checkpoint from the current attempt.</summary>
    public IEvent<ISaveJobCheckpoint> SaveJobCheckpoint { get; } = null!;
    /// <summary>Gets a request for the current job lifecycle snapshot.</summary>
    public IEvent<IGetJobState> GetJobState { get; } = null!;
    /// <summary>Gets the schedule used for capacity waits and recurring occurrences.</summary>
    public ISchedule<JobSaga, IJobSlotWaitElapsed> JobSlotWaitElapsed { get; } = null!;

    /// <summary>Gets the schedule used for a configured attempt retry delay.</summary>
    public ISchedule<JobSaga, IJobRetryDelayElapsed> JobRetryDelayElapsed { get; } = null!;
}

static class JobStateMachineBehaviorExtensions
{
    internal static string GetCancellationReason(this ICancelJob message)
    {
        return string.IsNullOrWhiteSpace(message.Reason) ? JobCancellationReasons.CancellationRequested : message.Reason;
    }

    internal static string GetCancellationReason(this IJobAttemptCanceled message)
    {
        return string.IsNullOrWhiteSpace(message.Reason) ? JobCancellationReasons.CancellationRequested : message.Reason;
    }

    internal static string GetCancellationReason(this ICancelJobAttempt message)
    {
        return string.IsNullOrWhiteSpace(message.Reason) ? JobCancellationReasons.CancellationRequested : message.Reason;
    }

    static Uri GetJobAttemptSagaAddress(this SagaConsumeContext<JobSaga> context)
    {
        return (context.GetPayload<IJobSagaSettings>()
            ?? throw new InvalidOperationException("The job saga settings payload is required.")).JobAttemptSagaEndpointAddress;
    }

    static Uri GetJobTypeSagaAddress(this SagaConsumeContext<JobSaga> context)
    {
        return (context.GetPayload<IJobSagaSettings>()
            ?? throw new InvalidOperationException("The job saga settings payload is required.")).JobTypeSagaEndpointAddress;
    }

    internal static bool IsScheduledJob(this SagaConsumeContext<JobSaga> context)
    {
        return !string.IsNullOrWhiteSpace(context.Saga.CronExpression) || context.Saga.StartDate is not null;
    }

    internal static void AddIncompleteAttempt(this SagaConsumeContext<JobSaga> context, Guid attemptId)
    {
        context.Saga.IncompleteAttempts ??= [];

        if (!context.Saga.IncompleteAttempts.Contains(attemptId))
            context.Saga.IncompleteAttempts.Add(attemptId);
    }

    public static bool CalculateNextStartDate(this SagaConsumeContext<JobSaga> context)
    {
        if (string.IsNullOrWhiteSpace(context.Saga.CronExpression))
        {
            if (context.Saga.StartDate is not null)
            {
                // Consuming an unchanged one-time start date does not create a new schedule.
                if (context.Saga.StartDate == context.Saga.NextStartDate)
                {
                    context.Saga.StartDate = null;
                    return false;
                }

                context.Saga.NextStartDate = context.Saga.StartDate.Value;
                context.Saga.StartDate = null;
                return true;
            }
        }

        var timeZone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(context.Saga.TimeZoneId))
        {
            var settings = context.GetPayload<IJobSagaSettings>()
                ?? throw new InvalidOperationException("The job saga settings payload is required.");
            timeZone = TimeZoneResolver.FindTimeZoneById(context.Saga.TimeZoneId, settings.TimeZoneResolver);
        }

        if (string.IsNullOrWhiteSpace(context.Saga.CronExpression))
        {
            context.Saga.NextStartDate = null;
            return false;
        }

        var cronExpression = new CronExpression(context.Saga.CronExpression) { TimeZone = timeZone };

        DateTimeOffset now = context.GetTimeProvider().GetUtcNow();

        DateTimeOffset? nextStartDate = cronExpression.GetTimeAfter(context.Saga.StartDate.HasValue
            ? context.Saga.StartDate.Value > now
                ? context.Saga.StartDate.Value
                : now
            : now);

        if (nextStartDate != null)
        {
            if (context.Saga.EndDate is DateTimeOffset endDate && nextStartDate.Value > endDate)
                nextStartDate = null;
        }

        // An unchanged occurrence requires no persistence or scheduler update.
        if (nextStartDate == context.Saga.NextStartDate)
            return false;

        context.Saga.NextStartDate = nextStartDate;
        return true;
    }

    public static IEventActivityBinder<JobSaga, IJobSubmitted> InitializeJob(this IEventActivityBinder<JobSaga, IJobSubmitted> binder)
    {
        return binder.Then(context =>
        {
            context.Saga.Submitted = context.Message.Timestamp;

            context.Saga.Job = JobPropertySnapshot.Create(context.Message.Job);
            context.Saga.ServiceAddress = context.SourceAddress
                ?? throw new InvalidOperationException("A source address is required when a job is submitted.");
            context.Saga.JobTimeout = context.Message.JobTimeout;
            context.Saga.JobTypeId = context.Message.JobTypeId;

            SetJobProperties(context);

            if (context.Message.Schedule != null)
            {
                context.Saga.CronExpression = context.Message.Schedule.CronExpression;
                context.Saga.TimeZoneId = context.Message.Schedule.TimeZoneId;
                context.Saga.StartDate = context.Message.Schedule.Start;
                context.Saga.EndDate = context.Message.Schedule.End;
            }

            context.Saga.AttemptId = NewId.NextGuid();
        });
    }

    public static IEventActivityBinder<JobSaga, IJobSubmitted> UpdateRecurringJob(this IEventActivityBinder<JobSaga, IJobSubmitted> binder)
    {
        return binder.Then(context =>
        {
            context.Saga.Job = JobPropertySnapshot.Create(context.Message.Job);

            if (context.Message.Schedule != null)
            {
                context.Saga.CronExpression = context.Message.Schedule.CronExpression;
                context.Saga.TimeZoneId = context.Message.Schedule.TimeZoneId;
                context.Saga.StartDate = context.Message.Schedule.Start;
                context.Saga.EndDate = context.Message.Schedule.End;
            }

            SetJobProperties(context);
        });
    }

    public static IEventActivityBinder<JobSaga, T> ClearNextStartDate<T>(this IEventActivityBinder<JobSaga, T> binder)
        where T : class
    {
        return binder.Then(context =>
        {
            context.Saga.NextStartDate = null;
        });
    }

    static void SetJobProperties(IBehaviorContext<JobSaga, IJobSubmitted> context)
    {
        context.Saga.JobProperties = context.Message.JobProperties is { } properties
            ? JobPropertySnapshot.Create(properties)
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    public static IEventActivityBinder<JobSaga, T> RequestJobSlot<T>(this IEventActivityBinder<JobSaga, T> binder, JobStateMachine machine)
        where T : class
    {
        return binder
            .Send<JobSaga, T, IAllocateJobSlot>(context => context.GetJobTypeSagaAddress(),
                context => new AllocateJobSlotCommand
                {
                    JobId = context.Saga.CorrelationId,
                    JobTypeId = context.Saga.JobTypeId,
                    JobTimeout = context.Saga.JobTimeout
                        ?? throw new InvalidOperationException("A job timeout is required before requesting capacity."),
                    JobProperties = context.Saga.JobProperties
                }, (behaviorContext, context) => context.ResponseAddress = behaviorContext.ReceiveContext.InputAddress)
            .TransitionTo(machine.AllocatingJobSlot);
    }

    public static IEventActivityBinder<JobSaga, T> RequestRetryJobSlot<T>(this IEventActivityBinder<JobSaga, T> binder, JobStateMachine machine)
        where T : class
    {
        return binder
            .Then(context =>
            {
                context.Saga.AttemptId = NewId.NextGuid();
                context.Saga.RetryAttempt++;
            })
            .RequestJobSlot(machine);
    }

    public static IEventActivityBinder<JobSaga, T> ClearJobState<T>(this IEventActivityBinder<JobSaga, T> binder)
        where T : class
    {
        return binder
            .Then(context =>
            {
                context.Saga.LastProgressValue = null;
                context.Saga.LastProgressLimit = null;
                context.Saga.LastProgressSequenceNumber = null;
                context.Saga.Checkpoint = null;
            });
    }

    public static IEventActivityBinder<JobSaga, IJobSlotAllocated> RequestStartJob(this IEventActivityBinder<JobSaga, IJobSlotAllocated> binder,
        JobStateMachine machine)
    {
        return binder
            .Send<JobSaga, IJobSlotAllocated, IStartJobAttempt>(context => context.GetJobAttemptSagaAddress(),
                context => new StartJobAttemptCommand
                {
                    JobId = context.Saga.CorrelationId,
                    AttemptId = context.Saga.AttemptId,
                    ServiceAddress = context.Saga.ServiceAddress,
                    InstanceAddress = context.Message.InstanceAddress,
                    RetryAttempt = context.Saga.RetryAttempt,
                    Job = context.Saga.Job,
                    JobTypeId = context.Saga.JobTypeId,
                    LastProgressValue = context.Saga.LastProgressValue,
                    LastProgressLimit = context.Saga.LastProgressLimit,
                    Checkpoint = context.Saga.Checkpoint,
                    JobProperties = context.Saga.JobProperties
                }, (behaviorContext, context) => context.ResponseAddress = behaviorContext.ReceiveContext.InputAddress)
            .TransitionTo(machine.StartingJobAttempt);
    }

    public static IEventActivityBinder<JobSaga, T> FinalizeJobAttempts<T>(this IEventActivityBinder<JobSaga, T> binder)
        where T : class
    {
        return binder.ThenAwaited(async context =>
        {
            if (context.Saga.IncompleteAttempts is { Count: > 0 })
            {
                var endpoint = await context.GetSendEndpointAsync(context.GetJobAttemptSagaAddress());

                foreach (var attemptId in context.Saga.IncompleteAttempts)
                {
                    await endpoint.SendAsync<IFinalizeJobAttempt>(new FinalizeJobAttemptCommand
                    {
                        JobId = context.Saga.CorrelationId,
                        AttemptId = attemptId
                    }, context.CancellationToken).ConfigureAwait(false);
                }

                context.Saga.IncompleteAttempts = null;
            }
        });
    }

    public static IEventActivityBinder<JobSaga, ICancelJob> CancelCurrentJobAttempt(this IEventActivityBinder<JobSaga, ICancelJob> binder)
    {
        return binder.Send<JobSaga, ICancelJob, ICancelJobAttempt>(context => context.GetJobAttemptSagaAddress(),
            context => new CancelJobAttemptCommand
            {
                JobId = context.Saga.CorrelationId,
                AttemptId = context.Saga.AttemptId,
                Reason = context.Message.GetCancellationReason()
            });
    }

    public static IEventActivityBinder<JobSaga, T> WaitForJobSlot<T>(this IEventActivityBinder<JobSaga, T> binder, JobStateMachine machine)
        where T : class
    {
        return binder.Schedule(machine.JobSlotWaitElapsed, context => new JobSlotWaitElapsedEvent { JobId = context.Saga.CorrelationId })
            .TransitionTo(machine.WaitingForSlot);
    }

    public static IEventActivityBinder<JobSaga, T> WaitForNextScheduledTime<T>(this IEventActivityBinder<JobSaga, T> binder, JobStateMachine machine)
        where T : class
    {
        return binder
            .ClearJobState()
            .Schedule(machine.JobSlotWaitElapsed, context => new JobSlotWaitElapsedEvent { JobId = context.Saga.CorrelationId },
                context => (context.Saga.NextStartDate
                    ?? throw new InvalidOperationException("The next start date is required to schedule the job.")),
                context => context.Headers.Set(DiagnosticPropagationHeaders.ParentMode, "Link"))
            .TransitionTo(machine.WaitingForSlot);
    }

    public static IEventActivityBinder<JobSaga, T> DetermineNextStartDate<T>(this IEventActivityBinder<JobSaga, T> binder)
        where T : class
    {
        return binder.Then(context =>
        {
            context.CalculateNextStartDate();
        });
    }

    public static IEventActivityBinder<JobSaga> SendJobSlotReleased(this IEventActivityBinder<JobSaga> binder, JobSlotDisposition disposition)
    {
        return binder.Send<JobSaga, IJobSlotReleased>(context => context.GetJobTypeSagaAddress(), context => new JobSlotReleasedEvent
        {
            JobId = context.Saga.CorrelationId,
            JobTypeId = context.Saga.JobTypeId,
            Disposition = disposition == JobSlotDisposition.Faulted && context.Saga.Reason?.Contains("(Suspect)") == true
                ? JobSlotDisposition.Suspect
                : disposition
        });
    }

    public static IEventActivityBinder<JobSaga, T> SendJobSlotReleased<T>(this IEventActivityBinder<JobSaga, T> binder, JobSlotDisposition disposition)
        where T : class
    {
        return binder.Send<JobSaga, T, IJobSlotReleased>(context => context.GetJobTypeSagaAddress(),
            context => new JobSlotReleasedEvent
            {
                JobId = context.Saga.CorrelationId,
                JobTypeId = context.Saga.JobTypeId,
                Disposition = disposition == JobSlotDisposition.Faulted && context.Saga.Reason?.Contains("(Suspect)") == true
                    ? JobSlotDisposition.Suspect
                    : disposition
            });
    }

    public static IEventActivityBinder<JobSaga, IJobAttemptCompleted> NotifyJobCompleted(this IEventActivityBinder<JobSaga, IJobAttemptCompleted> binder)
    {
        return binder
            .Send<JobSaga, IJobAttemptCompleted, ICompleteJob>(context => context.Saga.ServiceAddress,
                context => new CompleteJobCommand
                {
                    JobId = context.Saga.CorrelationId,
                    Job = context.Saga.Job,
                    JobTypeId = context.Saga.JobTypeId,
                    Timestamp = context.Message.Timestamp,
                    Duration = context.Message.Duration,
                    JobProperties = context.Saga.JobProperties,
                    InstanceProperties = context.Message.InstanceProperties,
                    JobTypeProperties = context.Message.JobTypeProperties
                })
            .Publish<JobSaga, IJobAttemptCompleted, IJobCompleted>(context => new JobCompletedEvent
            {
                JobId = context.Saga.CorrelationId,
                Job = context.Saga.Job,
                Timestamp = context.Message.Timestamp,
                Duration = context.Message.Duration,
                JobProperties = context.Saga.JobProperties,
                InstanceProperties = context.Message.InstanceProperties,
                JobTypeProperties = context.Message.JobTypeProperties
            });
    }

    public static IEventActivityBinder<JobSaga, IJobAttemptFaulted> NotifyJobFaulted(this IEventActivityBinder<JobSaga, IJobAttemptFaulted> binder)
    {
        return binder
            .Send<JobSaga, IJobAttemptFaulted, IFaultJob>(context => context.Saga.ServiceAddress,
                context => new FaultJobCommand
                {
                    JobId = context.Saga.CorrelationId,
                    Job = context.Saga.Job,
                    JobTypeId = context.Saga.JobTypeId,
                    AttemptId = context.Saga.AttemptId,
                    RetryAttempt = context.Saga.RetryAttempt,
                    Exceptions = context.Message.Exceptions,
                    Duration = context.Message.Timestamp - context.Saga.Started
                }, (context, sendContext) => sendContext.RequestId = context.Saga.CorrelationId)
            .Publish<JobSaga, IJobAttemptFaulted, IJobFaulted>(context => new JobFaultedEvent
            {
                JobId = context.Saga.CorrelationId,
                Job = context.Saga.Job,
                Exceptions = context.Message.Exceptions,
                Timestamp = context.Message.Timestamp,
                Duration = context.Message.Timestamp - context.Saga.Started
            });
    }

    public static IEventActivityBinder<JobSaga, T> PublishJobCanceled<T>(
        this IEventActivityBinder<JobSaga, T> binder,
        Func<IBehaviorContext<JobSaga, T>, string> getReason)
        where T : class
    {
        return binder
            .Then(context =>
            {
                context.Saga.Faulted = context.GetUtcNow();
                context.Saga.Reason = getReason(context);
            })
            .Publish<JobSaga, T, IJobCanceled>(context => new JobCanceledEvent
            {
                JobId = context.Saga.CorrelationId,
                Timestamp = context.Saga.Faulted
                    ?? throw new InvalidOperationException("The job fault timestamp must be set before publishing its cancellation."),
                Reason = context.Saga.Reason
            });
    }

    public static IEventActivityBinder<JobSaga, Fault<IStartJobAttempt>> NotifyJobFaulted(this IEventActivityBinder<JobSaga, Fault<IStartJobAttempt>> binder)
    {
        return binder
            .Send<JobSaga, Fault<IStartJobAttempt>, IFaultJob>(context => context.Saga.ServiceAddress,
                context => new FaultJobCommand
                {
                    JobId = context.Saga.CorrelationId,
                    Job = context.Saga.Job,
                    JobTypeId = context.Saga.JobTypeId,
                    AttemptId = context.Saga.AttemptId,
                    RetryAttempt = context.Saga.RetryAttempt,
                    Exceptions = context.Message.Exceptions.FirstOrDefault()
                        ?? throw new InvalidOperationException("A job attempt start fault must include exception details."),
                    Duration = context.Message.Timestamp - context.Saga.Started
                }, (context, sendContext) => sendContext.RequestId = context.Saga.CorrelationId)
            .Publish<JobSaga, Fault<IStartJobAttempt>, IJobFaulted>(context => new JobFaultedEvent
            {
                JobId = context.Saga.CorrelationId,
                Job = context.Saga.Job,
                Exceptions = context.Message.Exceptions.FirstOrDefault()
                    ?? throw new InvalidOperationException("A job attempt start fault must include exception details."),
                Timestamp = context.Message.Timestamp,
                Duration = context.Message.Timestamp - context.Saga.Started
            });
    }
}
