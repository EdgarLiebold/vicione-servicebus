using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService.Serialization;

internal static class JobServiceJsonTypeMappings
{
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "The optional capability must register its contract mappings before serializers inspect them.")]
    [ModuleInitializer]
    internal static void Register()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(SubmitJob<>), typeof(SubmitJobCommand<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(JobCompleted<>), typeof(JobCompletedEvent<>));
        JsonMessageTypeMappingRegistry.Register<JobSchedule, JobScheduleInfo>();
        JsonMessageTypeMappingRegistry.Register<AllocateJobSlot, AllocateJobSlotCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelJob, CancelJobCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelJobAttempt, CancelJobAttemptCommand>();
        JsonMessageTypeMappingRegistry.Register<CompleteJob, CompleteJobCommand>();
        JsonMessageTypeMappingRegistry.Register<FaultJob, FaultJobCommand>();
        JsonMessageTypeMappingRegistry.Register<FinalizeJob, FinalizeJobCommand>();
        JsonMessageTypeMappingRegistry.Register<FinalizeJobAttempt, FinalizeJobAttemptCommand>();
        JsonMessageTypeMappingRegistry.Register<GetJobAttemptStatus, GetJobAttemptStatusRequest>();
        JsonMessageTypeMappingRegistry.Register<GetJobState, GetJobStateRequest>();
        JsonMessageTypeMappingRegistry.Register<JobAttemptCanceled, JobAttemptCanceledEvent>();
        JsonMessageTypeMappingRegistry.Register<JobAttemptCompleted, JobAttemptCompletedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobAttemptFaulted, JobAttemptFaultedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobAttemptStarted, JobAttemptStartedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobCanceled, JobCanceledEvent>();
        JsonMessageTypeMappingRegistry.Register<JobCompleted, JobCompletedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobFaulted, JobFaultedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobRetryDelayElapsed, JobRetryDelayElapsedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobSlotAllocated, JobSlotAllocatedResponse>();
        JsonMessageTypeMappingRegistry.Register<JobSlotReleased, JobSlotReleasedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobSlotUnavailable, JobSlotUnavailableResponse>();
        JsonMessageTypeMappingRegistry.Register<JobSlotWaitElapsed, JobSlotWaitElapsedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobState, JobStateResponse>();
        JsonMessageTypeMappingRegistry.Register<JobStarted, JobStartedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobStatusCheckRequested, JobStatusCheckRequestedEvent>();
        JsonMessageTypeMappingRegistry.Register<JobSubmissionAccepted, JobSubmissionAcceptedResponse>();
        JsonMessageTypeMappingRegistry.Register<JobSubmitted, JobSubmittedEvent>();
        JsonMessageTypeMappingRegistry.Register<RetryJob, RetryJobCommand>();
        JsonMessageTypeMappingRegistry.Register<RunJob, RunJobCommand>();
        JsonMessageTypeMappingRegistry.Register<SaveJobCheckpoint, SaveJobCheckpointCommand>();
        JsonMessageTypeMappingRegistry.Register<SetConcurrentJobLimit, SetConcurrentJobLimitCommand>();
        JsonMessageTypeMappingRegistry.Register<SetJobProgress, SetJobProgressCommand>();
        JsonMessageTypeMappingRegistry.Register<StartJob, StartJobCommand>();
        JsonMessageTypeMappingRegistry.Register<StartJobAttempt, StartJobAttemptCommand>();
    }
}
