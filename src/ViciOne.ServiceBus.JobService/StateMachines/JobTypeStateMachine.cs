using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coordinates distributed execution capacity and instance health for one job type.</summary>
internal sealed class JobTypeStateMachine :
    ViciOneServiceBusStateMachine<JobTypeSaga>
{
    /// <summary>Initializes a new instance.</summary>
    public JobTypeStateMachine()
    {
        Event(() => JobSlotRequested, x =>
        {
            x.CorrelateById(m => m.Message.JobTypeId);
            x.ConfigureConsumeTopology = false;
        });
        Event(() => JobSlotReleased, x =>
        {
            x.CorrelateById(m => m.Message.JobTypeId);
            x.ConfigureConsumeTopology = false;
        });
        Event(() => SetConcurrentJobLimit, x => x.CorrelateById(m => m.Message.JobTypeId));

        InstanceState(x => x.CurrentState, Active, Idle);

        During(Initial, Active, Idle,
            When(JobSlotRequested)
                .IfElseAsync(context => context.IsSlotAvailableAsync(context.GetPayload<JobSagaSettings>().HeartbeatTimeout),
                    allocate => allocate
                        .TransitionTo(Active),
                    unavailable => unavailable
                        .Respond<JobTypeSaga, AllocateJobSlot, JobSlotUnavailable>(context =>
                            new JobSlotUnavailableResponse { JobId = context.Message.JobId })));

        During(Active,
            When(JobSlotReleased)
                .If(context => context.Saga.ActiveJobs.Any(x => x.JobId == context.Message.JobId),
                    release => release
                        .Then(context =>
                        {
                            var activeJob = context.Saga.ActiveJobs.FirstOrDefault(x => x.JobId == context.Message.JobId);
                            if (activeJob != null)
                            {
                                context.Saga.ActiveJobs.Remove(activeJob);
                                context.Saga.ActiveJobCount = context.Saga.ActiveJobs.Count;

                                LogContext.Debug?.Log("Released Job Slot: {JobId} ({JobCount}): {InstanceAddress}", activeJob.JobId,
                                    context.Saga.ActiveJobCount, activeJob.InstanceAddress);

                                if (context.Message.Disposition == JobSlotDisposition.Suspect)
                                {
                                    if (context.Saga.Instances.Remove(activeJob.InstanceAddress))
                                        LogContext.Warning?.Log("Removed Suspect Job Service Instance: {InstanceAddress}", activeJob.InstanceAddress);
                                }
                            }
                        }))
                .If(context => context.Saga.ActiveJobCount == 0,
                    empty => empty.TransitionTo(Idle)));

        During(Idle,
            Ignore(JobSlotReleased));

        During(Initial,
            When(SetConcurrentJobLimit)
                .SetConcurrentLimit()
                .TransitionTo(Idle));

        During(Active, Idle,
            When(SetConcurrentJobLimit)
                .SetConcurrentLimit()
        );
    }

    /// <summary>Gets the state in which at least one execution slot is allocated.</summary>
    public State Active { get; } = null!;
    /// <summary>Gets the state in which no execution slots are allocated.</summary>
    public State Idle { get; } = null!;

    /// <summary>Gets a request to reserve capacity for one job.</summary>
    public Event<AllocateJobSlot> JobSlotRequested { get; } = null!;
    /// <summary>Gets a notification that a previous allocation no longer consumes capacity.</summary>
    public Event<JobSlotReleased> JobSlotReleased { get; } = null!;
    /// <summary>Gets a configuration, heartbeat, override, or shutdown update from a service instance.</summary>
    public Event<SetConcurrentJobLimit> SetConcurrentJobLimit { get; } = null!;
}

static class JobTypeStateMachineBehaviorExtensions
{
    public static async Task<bool> IsSlotAvailableAsync(this BehaviorContext<JobTypeSaga, AllocateJobSlot> context, TimeSpan heartbeatTimeout)
    {
        if (context.Saga.OverrideLimitExpiration.HasValue)
        {
            if (context.Saga.OverrideLimitExpiration.Value <= context.GetUtcDateTime())
            {
                context.Saga.OverrideLimitExpiration = null;
                context.Saga.OverrideJobLimit = null;
            }
        }

        var timestamp = context.GetUtcDateTime();
        JobTypeCapacity.RemoveExpiredAllocations(context.Saga, timestamp, heartbeatTimeout);

        var jobId = context.Message.JobId;
        var activeJob = context.Saga.ActiveJobs.FirstOrDefault(x => x.JobId == jobId);
        if (activeJob != null)
        {
            await ((ConsumeContext<AllocateJobSlot>)context).RespondAsync<JobSlotAllocated>(new JobSlotAllocatedResponse
            {
                JobId = jobId,
                InstanceAddress = activeJob.InstanceAddress,
            });

            return true;
        }

        if (context.Saga.GlobalConcurrentJobLimit.HasValue && context.Saga.ActiveJobCount >= context.Saga.GlobalConcurrentJobLimit)
            return false;

        var strategy = context.GetJobDistributionStrategyOrUseDefault();

        Uri? selectedInstanceAddress = await strategy
            .SelectInstanceAsync(context, new JobTypeInfoSnapshot(context.Saga), context.CancellationToken)
            .ConfigureAwait(false);
        if (selectedInstanceAddress == null)
            return false;

        var activeInstance = context.Saga.Instances.TryGetValue(selectedInstanceAddress, out var value) ? value : null;
        if (activeInstance == null)
        {
            LogContext.Warning?.Log("Job Distribution Strategy returned unknown instance address: {InstanceAddress}", selectedInstanceAddress);
            return false;
        }

        activeInstance.Used = timestamp;

        activeJob = new ActiveJob
        {
            JobId = jobId,
            InstanceAddress = selectedInstanceAddress,
            Deadline = timestamp + context.Message.JobTimeout,
            JobProperties = JobTypeCapacity.CopyProperties(context.Message.JobProperties),
        };

        context.Saga.ActiveJobs.Add(activeJob);
        context.Saga.ActiveJobCount = context.Saga.ActiveJobs.Count;

        LogContext.Debug?.Log("Allocated Job Slot: {JobId} ({JobCount}): {InstanceAddress} ({InstanceCount})", jobId, context.Saga.ActiveJobCount,
            activeJob.InstanceAddress, context.Saga.ActiveJobs.Count(x => x.InstanceAddress == activeJob.InstanceAddress));

        await ((ConsumeContext<AllocateJobSlot>)context).RespondAsync<JobSlotAllocated>(new JobSlotAllocatedResponse
        {
            JobId = jobId,
            InstanceAddress = activeJob.InstanceAddress,
        });

        return true;
    }

    static IJobDistributionStrategy GetJobDistributionStrategyOrUseDefault(this ConsumeContext context)
    {
        IJobDistributionStrategy? strategy = null;

        if (context.TryGetPayload(out IServiceScope? serviceScope))
            strategy = serviceScope.ServiceProvider.GetService<IJobDistributionStrategy>();
        else if (context.TryGetPayload(out IServiceProvider? serviceProvider))
            strategy = serviceProvider.GetService<IJobDistributionStrategy>();

        return strategy ?? DefaultJobDistributionStrategy.Instance;
    }

    public static EventActivityBinder<JobTypeSaga, SetConcurrentJobLimit> SetConcurrentLimit(
        this EventActivityBinder<JobTypeSaga, SetConcurrentJobLimit> binder)
    {
        return binder.Then(context =>
        {
            ValidateConcurrencyUpdate(context.Message);

            var instanceAddress = context.Message.InstanceAddress;
            DateTimeOffset instanceUpdated = context.GetUtcDateTime();

            if (context.Saga.Instances.TryGetValue(instanceAddress, out var instance))
            {
                if (context.Message.UpdateKind == JobConcurrencyUpdateKind.InstanceStopped)
                {
                    LogContext.Debug?.Log("Job Service Instance Stopped: {InstanceAddress}", instanceAddress);
                    context.Saga.Instances.Remove(instanceAddress);
                }
                else if (instance.Updated is not DateTimeOffset lastUpdated || instanceUpdated > lastUpdated)
                    instance.Updated = instanceUpdated;
            }
            else if (context.Message.UpdateKind != JobConcurrencyUpdateKind.InstanceStopped)
            {
                instance = new JobTypeInstance { Updated = instanceUpdated };
                context.Saga.Instances.Add(instanceAddress, instance);
                LogContext.Debug?.Log("Job Service Instance Started: {InstanceAddress}", instanceAddress);
            }

            if (context.Message.UpdateKind != JobConcurrencyUpdateKind.InstanceStopped && instance != null)
                instance.InstanceProperties = JobTypeCapacity.CopyProperties(context.Message.InstanceProperties);

            if (context.Message.UpdateKind == JobConcurrencyUpdateKind.Configuration)
            {
                context.Saga.ConcurrentJobLimit = context.Message.ConcurrentJobLimit;
                context.Saga.GlobalConcurrentJobLimit = context.Message.GlobalConcurrentJobLimit;
                context.Saga.Name = context.Message.JobTypeName!;
                context.Saga.JobTypeProperties = JobTypeCapacity.CopyProperties(context.Message.JobTypeProperties);

                LogContext.Debug?.Log("Concurrent Job Limit: {ConcurrencyLimit} {JobTypeName}", context.Saga.ConcurrentJobLimit,
                    context.Message.JobTypeName);
            }
            else if (context.Message.UpdateKind == JobConcurrencyUpdateKind.TemporaryOverride)
            {
                context.Saga.OverrideJobLimit = context.Message.ConcurrentJobLimit;
                context.Saga.OverrideLimitExpiration = instanceUpdated + (context.Message.Duration ?? TimeSpan.FromMinutes(30));

                LogContext.Debug?.Log("Override Concurrent Job Limit: {ConcurrencyLimit}", context.Saga.OverrideJobLimit);
            }
        });
    }

    internal static void ValidateConcurrencyUpdate(SetConcurrentJobLimit message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.JobTypeId == Guid.Empty)
            throw new MessageException(typeof(SetConcurrentJobLimit), "The job type identifier must not be empty.");
        if (message.InstanceAddress == null)
            throw new MessageException(typeof(SetConcurrentJobLimit), "The service instance address is required.");
        if (!Enum.IsDefined(message.UpdateKind))
            throw new MessageException(typeof(SetConcurrentJobLimit), "The concurrency update kind is not defined.");

        if (message.UpdateKind is JobConcurrencyUpdateKind.Configuration or JobConcurrencyUpdateKind.TemporaryOverride)
        {
            if (message.ConcurrentJobLimit <= 0)
                throw new MessageException(typeof(SetConcurrentJobLimit), "The concurrent job limit must be positive.");
            if (message.GlobalConcurrentJobLimit is <= 0)
                throw new MessageException(typeof(SetConcurrentJobLimit), "The global concurrent job limit must be positive when specified.");
        }

        if (message.UpdateKind == JobConcurrencyUpdateKind.Configuration && string.IsNullOrWhiteSpace(message.JobTypeName))
            throw new MessageException(typeof(SetConcurrentJobLimit), "A job type name is required for a configuration update.");
        if (message.UpdateKind == JobConcurrencyUpdateKind.TemporaryOverride && message.Duration is TimeSpan duration && duration <= TimeSpan.Zero)
            throw new MessageException(typeof(SetConcurrentJobLimit), "The override duration must be positive when specified.");
    }
}
