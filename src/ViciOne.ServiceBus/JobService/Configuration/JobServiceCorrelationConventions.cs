using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

static class JobServiceCorrelationConventions
{
    static readonly object Sync = new();
    static bool _registered;

    internal static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            GlobalTopology.UseCapabilityCorrelationId<AllocateJobSlot>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<JobSlotReleased>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<SetConcurrentJobLimit>(message => message.JobTypeId);
            GlobalTopology.UseCapabilityCorrelationId<CancelJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<AllocateJobSlot>>(message => message.Message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<StartJobAttempt>>(message => message.Message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<FinalizeJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<GetJobState>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobAttemptCanceled>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobAttemptCompleted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobAttemptFaulted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobAttemptStarted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobCanceled>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobCompleted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobRetryDelayElapsed>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobSlotAllocated>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobSlotUnavailable>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobSlotWaitElapsed>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<JobSubmitted>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<RetryJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<RunJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<SaveJobState>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<SetJobProgress>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<StartJob>(message => message.JobId);
            GlobalTopology.UseCapabilityCorrelationId<StartJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<FinalizeJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<CancelJobAttempt>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<Fault<StartJob>>(message => message.Message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<JobAttemptStatus>(message => message.AttemptId);
            GlobalTopology.UseCapabilityCorrelationId<JobStatusCheckRequested>(message => message.AttemptId);

            _registered = true;
        }
    }
}
