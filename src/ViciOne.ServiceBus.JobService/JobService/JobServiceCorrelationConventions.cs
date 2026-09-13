using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

internal static class JobServiceCorrelationConventions
{
    static readonly Lock Sync = new();
    static bool _registered;

    internal static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            GlobalTopology.UseCapabilityCorrelationId<IAllocateJobSlot>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<IJobSlotReleased>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<ISetConcurrentJobLimit>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<ICancelJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<IAllocateJobSlot>>(message => message.Message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<IStartJobAttempt>>(message => message.Message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IFinalizeJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IGetJobState>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobAttemptCanceled>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobAttemptCompleted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobAttemptFaulted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobAttemptStarted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobCanceled>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobCompleted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobRetryDelayElapsed>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobSlotAllocated>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobSlotUnavailable>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobSlotWaitElapsed>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IJobSubmitted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IRetryJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IRunJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<ISaveJobCheckpoint>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<ISetJobProgress>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IStartJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<IStartJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<IFinalizeJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<ICancelJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<IStartJob>>(message => message.Message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<IJobAttemptStatus>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<IJobStatusCheckRequested>(message => message.AttemptId);

            _registered = true;
        }
    }
}
