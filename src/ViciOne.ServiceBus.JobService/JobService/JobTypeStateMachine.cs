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
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coordinates distributed execution capacity and instance health for one job type.</summary>
internal sealed class JobTypeStateMachine :
    ViciOneServiceBusStateMachine<JobTypeSaga>
{
    /// <summary>Defines capacity allocation, release, heartbeat, and concurrency-update behavior.</summary>
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
                .IfElseAwaited(context => context.IsSlotAvailableAsync(context.GetPayload<IJobSagaSettings>().HeartbeatTimeout),
                    allocate => allocate
                        .TransitionTo(Active),
                    unavailable => unavailable
                        .Respond<JobTypeSaga, IAllocateJobSlot, IJobSlotUnavailable>(context =>
                            new JobSlotUnavailableResponse { JobId = context.Message.JobId })));

        During(Active,
            When(JobSlotReleased)
                .If(context => context.Saga.ActiveAllocations.Any(x => x.JobId == context.Message.JobId),
                    release => release
                        .Then(context =>
                        {
                            var allocation = context.Saga.ActiveAllocations.FirstOrDefault(x => x.JobId == context.Message.JobId);
                            if (allocation != null)
                            {
                                context.Saga.ActiveAllocations.Remove(allocation);
                                context.Saga.ActiveAllocationCount = context.Saga.ActiveAllocations.Count;

                                LogContext.Debug?.Log("Released Job Slot: {JobId} ({JobCount}): {InstanceAddress}", allocation.JobId,
                                    context.Saga.ActiveAllocationCount, allocation.InstanceAddress);

                                if (context.Message.Disposition == JobSlotDisposition.Suspect)
                                {
                                    if (context.Saga.ServiceInstances.Remove(allocation.InstanceAddress))
                                        LogContext.Warning?.Log("Removed Suspect Job Service Instance: {InstanceAddress}", allocation.InstanceAddress);
                                }
                            }
                        }))
                .If(context => context.Saga.ActiveAllocationCount == 0,
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
    public IState Active { get; } = null!;
    /// <summary>Gets the state in which no execution slots are allocated.</summary>
    public IState Idle { get; } = null!;

    /// <summary>Gets a request to reserve capacity for one job.</summary>
    public IEvent<IAllocateJobSlot> JobSlotRequested { get; } = null!;
    /// <summary>Gets a notification that a previous allocation no longer consumes capacity.</summary>
    public IEvent<IJobSlotReleased> JobSlotReleased { get; } = null!;
    /// <summary>Gets a configuration, heartbeat, override, or shutdown update from a service instance.</summary>
    public IEvent<ISetConcurrentJobLimit> SetConcurrentJobLimit { get; } = null!;
}

static class JobTypeStateMachineBehaviorExtensions
{
    public static async Task<bool> IsSlotAvailableAsync(this IBehaviorContext<JobTypeSaga, IAllocateJobSlot> context, TimeSpan heartbeatTimeout)
    {
        if (context.Saga.OverrideExpiresAt.HasValue)
        {
            if (context.Saga.OverrideExpiresAt.Value <= context.GetUtcNow())
            {
                context.Saga.OverrideExpiresAt = null;
                context.Saga.OverrideConcurrentJobLimit = null;
            }
        }

        var timestamp = context.GetUtcNow();
        JobTypeCapacity.RemoveExpiredAllocations(context.Saga, timestamp, heartbeatTimeout);

        var jobId = context.Message.JobId;
        var allocation = context.Saga.ActiveAllocations.FirstOrDefault(x => x.JobId == jobId);
        if (allocation != null)
        {
            await ((ConsumeContext<IAllocateJobSlot>)context).RespondAsync<IJobSlotAllocated>(new JobSlotAllocatedResponse
            {
                JobId = jobId,
                InstanceAddress = allocation.InstanceAddress,
            });

            return true;
        }

        if (context.Saga.GlobalConcurrentJobLimit.HasValue && context.Saga.ActiveAllocationCount >= context.Saga.GlobalConcurrentJobLimit)
            return false;

        var strategy = context.GetJobDistributionStrategyOrUseDefault();

        Uri? selectedInstanceAddress = await strategy
            .SelectInstanceAsync(context, new JobDistributionContext(context.Saga), context.CancellationToken)
            .ConfigureAwait(false);
        if (selectedInstanceAddress == null)
            return false;

        var activeInstance = context.Saga.ServiceInstances.TryGetValue(selectedInstanceAddress, out var value) ? value : null;
        if (activeInstance == null)
        {
            LogContext.Warning?.Log("Job Distribution Strategy returned unknown instance address: {InstanceAddress}", selectedInstanceAddress);
            return false;
        }

        activeInstance.LastAllocationAt = timestamp;

        allocation = new JobAllocationState
        {
            JobId = jobId,
            InstanceAddress = selectedInstanceAddress,
            ExpiresAt = timestamp + context.Message.JobTimeout,
            Properties = JobPropertySnapshot.Create(context.Message.JobProperties),
        };

        context.Saga.ActiveAllocations.Add(allocation);
        context.Saga.ActiveAllocationCount = context.Saga.ActiveAllocations.Count;

        LogContext.Debug?.Log("Allocated Job Slot: {JobId} ({JobCount}): {InstanceAddress} ({InstanceCount})", jobId,
            context.Saga.ActiveAllocationCount, allocation.InstanceAddress,
            context.Saga.ActiveAllocations.Count(x => x.InstanceAddress == allocation.InstanceAddress));

        await ((ConsumeContext<IAllocateJobSlot>)context).RespondAsync<IJobSlotAllocated>(new JobSlotAllocatedResponse
        {
            JobId = jobId,
            InstanceAddress = allocation.InstanceAddress,
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

    public static IEventActivityBinder<JobTypeSaga, ISetConcurrentJobLimit> SetConcurrentLimit(
        this IEventActivityBinder<JobTypeSaga, ISetConcurrentJobLimit> binder)
    {
        return binder.Then(context =>
        {
            ValidateConcurrencyUpdate(context.Message);

            var instanceAddress = context.Message.InstanceAddress;
            DateTimeOffset instanceUpdated = context.GetUtcNow();

            if (context.Saga.ServiceInstances.TryGetValue(instanceAddress, out var instance))
            {
                if (context.Message.UpdateKind == JobConcurrencyUpdateKind.InstanceStopped)
                {
                    LogContext.Debug?.Log("Job Service Instance Stopped: {InstanceAddress}", instanceAddress);
                    context.Saga.ServiceInstances.Remove(instanceAddress);
                }
                else if (instance.LastHeartbeatAt is not DateTimeOffset lastHeartbeatAt || instanceUpdated > lastHeartbeatAt)
                    instance.LastHeartbeatAt = instanceUpdated;
            }
            else if (context.Message.UpdateKind != JobConcurrencyUpdateKind.InstanceStopped)
            {
                instance = new JobServiceInstanceState { LastHeartbeatAt = instanceUpdated };
                context.Saga.ServiceInstances.Add(instanceAddress, instance);
                LogContext.Debug?.Log("Job Service Instance Started: {InstanceAddress}", instanceAddress);
            }

            if (context.Message.UpdateKind != JobConcurrencyUpdateKind.InstanceStopped && instance != null)
                instance.Properties = JobPropertySnapshot.Create(context.Message.InstanceProperties);

            if (context.Message.UpdateKind == JobConcurrencyUpdateKind.Configuration)
            {
                context.Saga.ConcurrentJobLimit = context.Message.ConcurrentJobLimit;
                context.Saga.GlobalConcurrentJobLimit = context.Message.GlobalConcurrentJobLimit;
                context.Saga.Name = context.Message.JobTypeName!;
                context.Saga.JobTypeProperties = JobPropertySnapshot.Create(context.Message.JobTypeProperties);

                LogContext.Debug?.Log("Concurrent Job Limit: {ConcurrencyLimit} {JobTypeName}", context.Saga.ConcurrentJobLimit,
                    context.Message.JobTypeName);
            }
            else if (context.Message.UpdateKind == JobConcurrencyUpdateKind.TemporaryOverride)
            {
                context.Saga.OverrideConcurrentJobLimit = context.Message.ConcurrentJobLimit;
                context.Saga.OverrideExpiresAt = instanceUpdated + (context.Message.Duration ?? TimeSpan.FromMinutes(30));

                LogContext.Debug?.Log("Override Concurrent Job Limit: {ConcurrencyLimit}", context.Saga.OverrideConcurrentJobLimit);
            }
        });
    }

    internal static void ValidateConcurrencyUpdate(ISetConcurrentJobLimit message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.JobTypeId == Guid.Empty)
            throw new MessageException(typeof(ISetConcurrentJobLimit), "The job type identifier must not be empty.");
        if (message.InstanceAddress == null)
            throw new MessageException(typeof(ISetConcurrentJobLimit), "The service instance address is required.");
        if (!Enum.IsDefined(message.UpdateKind))
            throw new MessageException(typeof(ISetConcurrentJobLimit), "The concurrency update kind is not defined.");

        if (message.UpdateKind is JobConcurrencyUpdateKind.Configuration or JobConcurrencyUpdateKind.TemporaryOverride)
        {
            if (message.ConcurrentJobLimit <= 0)
                throw new MessageException(typeof(ISetConcurrentJobLimit), "The concurrent job limit must be positive.");
            if (message.GlobalConcurrentJobLimit is <= 0)
                throw new MessageException(typeof(ISetConcurrentJobLimit), "The global concurrent job limit must be positive when specified.");
        }

        if (message.UpdateKind == JobConcurrencyUpdateKind.Configuration && string.IsNullOrWhiteSpace(message.JobTypeName))
            throw new MessageException(typeof(ISetConcurrentJobLimit), "A job type name is required for a configuration update.");
        if (message.UpdateKind == JobConcurrencyUpdateKind.TemporaryOverride && message.Duration is TimeSpan duration && duration <= TimeSpan.Zero)
            throw new MessageException(typeof(ISetConcurrentJobLimit), "The override duration must be positive when specified.");
    }
}
