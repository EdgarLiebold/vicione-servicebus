using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Supervises one execution attempt and detects unresponsive service instances.</summary>
internal sealed class JobAttemptStateMachine :
    ViciOneServiceBusStateMachine<JobAttemptSaga>
{
    /// <summary>Defines attempt startup, liveness supervision, cancellation, and finalization behavior.</summary>
    public JobAttemptStateMachine()
    {
        Event(() => StartJobAttempt, x =>
        {
            x.CorrelateById(context => context.Message.AttemptId);
            x.ConfigureConsumeTopology = false;
        });
        Event(() => StartJobFaulted, x =>
        {
            x.CorrelateById(context => context.Message.Message.AttemptId);
            x.ConfigureConsumeTopology = false;
        });
        Event(() => FinalizeJobAttempt, x =>
        {
            x.CorrelateById(context => context.Message.AttemptId);
            x.ConfigureConsumeTopology = false;
        });
        Event(() => CancelJobAttempt, x =>
        {
            x.CorrelateById(context => context.Message.AttemptId);
            x.ConfigureConsumeTopology = false;
        });

        Event(() => AttemptCanceled, x => x.CorrelateById(context => context.Message.AttemptId));
        Event(() => AttemptCompleted, x => x.CorrelateById(context => context.Message.AttemptId));
        Event(() => AttemptFaulted, x => x.CorrelateById(context => context.Message.AttemptId));
        Event(() => AttemptStarted, x => x.CorrelateById(context => context.Message.AttemptId));

        Event(() => AttemptStatus, x =>
        {
            x.CorrelateById(context => context.Message.AttemptId);
            x.ConfigureConsumeTopology = false;
        });

        Schedule(() => StatusCheckRequested, instance => instance.StatusCheckTokenId, x =>
        {
            x.DelayProvider = context => (context.GetPayload<IJobSagaSettings>()
                ?? throw new InvalidOperationException("The job saga settings payload is required.")).StatusCheckInterval;
            x.Received = r =>
            {
                r.CorrelateById(context => context.Message.AttemptId);
                r.ConfigureConsumeTopology = false;
            };
        });

        InstanceState(x => x.CurrentState, Starting, Running, Faulted, CheckingStatus, Suspect);

        During(Initial, Starting,
            When(StartJobAttempt)
                .Then(context =>
                {
                    context.Saga.JobId = context.Message.JobId;
                    context.Saga.RetryAttempt = context.Message.RetryAttempt;
                    context.Saga.InstanceAddress ??= context.Message.InstanceAddress;
                    context.Saga.ServiceAddress ??= context.Message.ServiceAddress;
                })
                .ScheduleJobStatusCheck(this)
                .SendStartJob()
                .TransitionTo(Starting));

        During(Starting,
            When(StartJobFaulted)
                .Then(context =>
                {
                    context.Saga.Faulted = context.Message.Timestamp;
                    context.Saga.InstanceAddress ??= context.SourceAddress
                        ?? throw new InvalidOperationException("A source address is required when a job attempt start faults.");
                })
                .SendJobAttemptFaulted()
                .TransitionTo(Faulted));

        During(Starting,
            When(StatusCheckRequested.Received)
                .SendJobAttemptStartTimeout()
                .TransitionTo(Faulted));

        During(Starting, Running,
            When(AttemptStarted)
                .Then(context =>
                {
                    context.Saga.JobId = context.Message.JobId;
                    context.Saga.Started = context.Message.Timestamp;
                    context.Saga.RetryAttempt = context.Message.RetryAttempt;
                    context.Saga.InstanceAddress ??= context.Message.InstanceAddress;
                })
                .TransitionTo(Running));

        During(Faulted,
            When(AttemptStarted)
                .Then(context =>
                {
                    context.Saga.Started = context.Message.Timestamp;
                    context.Saga.RetryAttempt = context.Message.RetryAttempt;
                    context.Saga.InstanceAddress ??= context.Message.InstanceAddress;
                }));

        During(Starting, Running, CheckingStatus, Suspect,
            When(AttemptCompleted)
                .Unschedule(StatusCheckRequested)
                .Finalize(),
            When(AttemptCanceled)
                .Unschedule(StatusCheckRequested)
                .Finalize(),
            When(CancelJobAttempt)
                .SendCancelJobAttempt(),
            When(AttemptFaulted)
                .Then(context =>
                {
                    context.Saga.Faulted = context.Message.Timestamp;
                    context.Saga.InstanceAddress ??= context.SourceAddress
                        ?? throw new InvalidOperationException("A source address is required when a job attempt faults.");
                })
                .Unschedule(StatusCheckRequested)
                .TransitionTo(Faulted));

        During(Running,
            When(StatusCheckRequested.Received)
                .ScheduleJobStatusCheck(this)
                .SendCheckJobStatus()
                .TransitionTo(CheckingStatus)
        );

        During(CheckingStatus,
            When(StatusCheckRequested.Received)
                .ScheduleJobStatusCheck(this)
                .SendCheckJobStatus()
                .TransitionTo(Suspect)
        );

        During(Running, CheckingStatus, Suspect,
            When(AttemptStatus, context => context.Message.Status == JobAttemptStatusKind.Running)
                .TransitionTo(Running),
            When(AttemptStatus, context => context.Message.Status == JobAttemptStatusKind.Canceled || context.Message.Status == JobAttemptStatusKind.Completed)
                .Unschedule(StatusCheckRequested)
                .Finalize(),
            When(AttemptStatus, context => context.Message.Status == JobAttemptStatusKind.Faulted)
                .Unschedule(StatusCheckRequested)
                .TransitionTo(Faulted));

        During(Suspect,
            When(StatusCheckRequested.Received)
                .SendJobAttemptFaulted()
                .TransitionTo(Faulted));

        During(Faulted,
            Ignore(StatusCheckRequested.Received),
            Ignore(AttemptStatus),
            Ignore(AttemptFaulted),
            Ignore(AttemptCompleted),
            Ignore(AttemptCanceled));

        During([Initial, Faulted, CheckingStatus, Suspect],
            When(FinalizeJobAttempt)
                .Finalize());

        During(Initial,
            When(AttemptCompleted)
                .Finalize(),
            When(AttemptCanceled)
                .Finalize(),
            When(StatusCheckRequested.Received)
                .Finalize(),
            When(AttemptStatus)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    /// <summary>Gets the state in which the execution command has been sent but the consumer has not acknowledged it.</summary>
    public IState Starting { get; } = null!;
    /// <summary>Gets the state in which the consumer is executing the attempt.</summary>
    public IState Running { get; } = null!;
    /// <summary>Gets the state in which the first liveness request is awaiting a response.</summary>
    public IState CheckingStatus { get; } = null!;
    /// <summary>Gets the state in which a second unanswered liveness request makes the attempt suspect.</summary>
    public IState Suspect { get; } = null!;
    /// <summary>Gets the state in which the attempt has failed and awaits explicit finalization.</summary>
    public IState Faulted { get; } = null!;

    /// <summary>Gets the command that creates and starts an execution attempt.</summary>
    public IEvent<IStartJobAttempt> StartJobAttempt { get; } = null!;
    /// <summary>Gets the fault emitted when the local execution command cannot be delivered or consumed.</summary>
    public IEvent<Fault<IStartJob>> StartJobFaulted { get; } = null!;
    /// <summary>Gets the command that removes persisted attempt state.</summary>
    public IEvent<IFinalizeJobAttempt> FinalizeJobAttempt { get; } = null!;
    /// <summary>Gets the command that requests cancellation of the active local execution.</summary>
    public IEvent<ICancelJobAttempt> CancelJobAttempt { get; } = null!;
    /// <summary>Gets the consumer acknowledgement that execution has started.</summary>
    public IEvent<IJobAttemptStarted> AttemptStarted { get; } = null!;
    /// <summary>Gets the notification that execution has faulted.</summary>
    public IEvent<IJobAttemptFaulted> AttemptFaulted { get; } = null!;
    /// <summary>Gets the notification that execution has completed successfully.</summary>
    public IEvent<IJobAttemptCompleted> AttemptCompleted { get; } = null!;
    /// <summary>Gets the notification that execution has been canceled.</summary>
    public IEvent<IJobAttemptCanceled> AttemptCanceled { get; } = null!;
    /// <summary>Gets the response to an active liveness request.</summary>
    public IEvent<IJobAttemptStatus> AttemptStatus { get; } = null!;
    /// <summary>Gets the recurring liveness-check schedule.</summary>
    public ISchedule<JobAttemptSaga, IJobStatusCheckRequested> StatusCheckRequested { get; } = null!;
}

static class JobAttemptStateMachineBehaviorExtensions
{
    public static TimeSpan? GetRetryDelay<T>(this IBehaviorContext<JobAttemptSaga, T> context)
        where T : class
    {
        var settings = context.GetPayload<IJobSagaSettings>()
            ?? throw new InvalidOperationException("The job saga settings payload is required.");

        return context.Saga.RetryAttempt < settings.SuspectJobRetryCount
            ? settings.SuspectJobRetryDelay ?? settings.SlotWaitTime
            : null;
    }

    static Uri GetJobSagaAddress(this SagaConsumeContext<JobAttemptSaga> context)
    {
        return (context.GetPayload<IJobSagaSettings>()
            ?? throw new InvalidOperationException("The job saga settings payload is required.")).JobSagaEndpointAddress;
    }

    static Uri GetJobAttemptSagaAddress(this SagaConsumeContext<JobAttemptSaga> context)
    {
        return (context.GetPayload<IJobSagaSettings>()
            ?? throw new InvalidOperationException("The job saga settings payload is required.")).JobAttemptSagaEndpointAddress;
    }

    public static IEventActivityBinder<JobAttemptSaga, IStartJobAttempt> SendStartJob(this IEventActivityBinder<JobAttemptSaga, IStartJobAttempt> binder)
    {
        return binder.Send<JobAttemptSaga, IStartJobAttempt, IStartJob>(context => context.Saga.InstanceAddress ?? context.Saga.ServiceAddress,
            context => new StartJobCommand
            {
                JobId = context.Message.JobId,
                AttemptId = context.Message.AttemptId,
                RetryAttempt = context.Message.RetryAttempt,
                Job = context.Message.Job,
                JobTypeId = context.Message.JobTypeId,
                LastProgressValue = context.Message.LastProgressValue,
                LastProgressLimit = context.Message.LastProgressLimit,
                Checkpoint = context.Message.Checkpoint,
                JobProperties = context.Message.JobProperties
            }, (behaviorContext, context) => context.FaultAddress = behaviorContext.GetJobAttemptSagaAddress());
    }

    public static IEventActivityBinder<JobAttemptSaga, IJobStatusCheckRequested> SendCheckJobStatus(this IEventActivityBinder<JobAttemptSaga,
        IJobStatusCheckRequested> binder)
    {
        return binder.Send<JobAttemptSaga, IJobStatusCheckRequested, IGetJobAttemptStatus>(
            context => context.Saga.InstanceAddress ?? context.Saga.ServiceAddress, context => new GetJobAttemptStatusRequest
            {
                JobId = context.Saga.JobId,
                AttemptId = context.Saga.CorrelationId
            }, (behaviorContext, context) =>
            {
                context.RequestId = behaviorContext.Saga.CorrelationId;
                context.ResponseAddress = behaviorContext.GetJobAttemptSagaAddress();
            });
    }

    public static IEventActivityBinder<JobAttemptSaga, ICancelJobAttempt> SendCancelJobAttempt(this IEventActivityBinder<JobAttemptSaga,
        ICancelJobAttempt> binder)
    {
        return binder.Send(context => context.Saga.InstanceAddress ?? context.Saga.ServiceAddress,
            context => context.Message,
            (behaviorContext, context) =>
            {
                context.RequestId = behaviorContext.Saga.CorrelationId;
                context.ResponseAddress = behaviorContext.GetJobAttemptSagaAddress();
            });
    }

    public static IEventActivityBinder<JobAttemptSaga, T> ScheduleJobStatusCheck<T>(this IEventActivityBinder<JobAttemptSaga, T> binder,
        JobAttemptStateMachine machine)
        where T : class
    {
        return binder.Schedule(machine.StatusCheckRequested, x => new JobStatusCheckRequestedEvent
        {
            AttemptId = x.Saga.CorrelationId,
            JobId = x.Saga.JobId
        });
    }

    public static IEventActivityBinder<JobAttemptSaga, Fault<IStartJob>> SendJobAttemptFaulted(
        this IEventActivityBinder<JobAttemptSaga, Fault<IStartJob>> binder)
    {
        return binder.Send<JobAttemptSaga, Fault<IStartJob>, IJobAttemptFaulted>(context => context.GetJobSagaAddress(),
            context => new JobAttemptFaultedEvent
            {
                JobId = context.Saga.JobId,
                AttemptId = context.Saga.CorrelationId,
                RetryAttempt = context.Saga.RetryAttempt,
                Timestamp = context.Message.Timestamp,
                Exceptions = context.Message.Exceptions.FirstOrDefault()
                    ?? throw new InvalidOperationException("A job start fault must include exception details.")
            });
    }

    public static IEventActivityBinder<JobAttemptSaga, T> SendJobAttemptFaulted<T>(this IEventActivityBinder<JobAttemptSaga, T> binder)
        where T : class
    {
        return binder.Send<JobAttemptSaga, T, IJobAttemptFaulted>(context => context.GetJobSagaAddress(),
            context => new JobAttemptFaultedEvent
            {
                JobId = context.Saga.JobId,
                AttemptId = context.Saga.CorrelationId,
                RetryAttempt = context.Saga.RetryAttempt,
                Timestamp = context.GetUtcNow(),
                RetryDelay = context.GetRetryDelay(),
                Exceptions = new FaultExceptionInfo(new TimeoutException("The job status check timed out."))
            });
    }

    public static IEventActivityBinder<JobAttemptSaga, T> SendJobAttemptStartTimeout<T>(this IEventActivityBinder<JobAttemptSaga, T> binder)
        where T : class
    {
        return binder.Send<JobAttemptSaga, T, IJobAttemptFaulted>(context => context.GetJobSagaAddress(),
            context => new JobAttemptFaultedEvent
            {
                JobId = context.Saga.JobId,
                AttemptId = context.Saga.CorrelationId,
                RetryAttempt = context.Saga.RetryAttempt,
                Timestamp = context.GetUtcNow(),
                RetryDelay = context.GetRetryDelay(),
                Exceptions = new FaultExceptionInfo(new TimeoutException($"The job service failed to respond: {context.Saga.InstanceAddress} (Suspect)"))
            });
    }
}
