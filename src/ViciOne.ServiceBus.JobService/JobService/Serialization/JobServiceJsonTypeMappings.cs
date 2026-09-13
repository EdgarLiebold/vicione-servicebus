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
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(ISubmitJob<>), typeof(SubmitJobCommand<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(IJobCompleted<>), typeof(JobCompletedEvent<>));
        JsonMessageTypeMappingRegistry.Register<IJobSchedule, JobScheduleInfo>();
        JsonMessageTypeMappingRegistry.Register<IAllocateJobSlot, AllocateJobSlotCommand>();
        JsonMessageTypeMappingRegistry.Register<ICancelJob, CancelJobCommand>();
        JsonMessageTypeMappingRegistry.Register<ICancelJobAttempt, CancelJobAttemptCommand>();
        JsonMessageTypeMappingRegistry.Register<ICompleteJob, CompleteJobCommand>();
        JsonMessageTypeMappingRegistry.Register<IFaultJob, FaultJobCommand>();
        JsonMessageTypeMappingRegistry.Register<IFinalizeJob, FinalizeJobCommand>();
        JsonMessageTypeMappingRegistry.Register<IFinalizeJobAttempt, FinalizeJobAttemptCommand>();
        JsonMessageTypeMappingRegistry.Register<IGetJobAttemptStatus, GetJobAttemptStatusRequest>();
        JsonMessageTypeMappingRegistry.Register<IGetJobState, GetJobStateRequest>();
        JsonMessageTypeMappingRegistry.Register<IJobAttemptCanceled, JobAttemptCanceledEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobAttemptCompleted, JobAttemptCompletedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobAttemptFaulted, JobAttemptFaultedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobAttemptStarted, JobAttemptStartedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobCanceled, JobCanceledEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobCompleted, JobCompletedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobFaulted, JobFaultedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobRetryDelayElapsed, JobRetryDelayElapsedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobSlotAllocated, JobSlotAllocatedResponse>();
        JsonMessageTypeMappingRegistry.Register<IJobSlotReleased, JobSlotReleasedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobSlotUnavailable, JobSlotUnavailableResponse>();
        JsonMessageTypeMappingRegistry.Register<IJobSlotWaitElapsed, JobSlotWaitElapsedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobState, JobStateResponse>();
        JsonMessageTypeMappingRegistry.Register<IJobStarted, JobStartedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobStatusCheckRequested, JobStatusCheckRequestedEvent>();
        JsonMessageTypeMappingRegistry.Register<IJobSubmissionAccepted, JobSubmissionAcceptedResponse>();
        JsonMessageTypeMappingRegistry.Register<IJobSubmitted, JobSubmittedEvent>();
        JsonMessageTypeMappingRegistry.Register<IRetryJob, RetryJobCommand>();
        JsonMessageTypeMappingRegistry.Register<IRunJob, RunJobCommand>();
        JsonMessageTypeMappingRegistry.Register<ISaveJobCheckpoint, SaveJobCheckpointCommand>();
        JsonMessageTypeMappingRegistry.Register<ISetConcurrentJobLimit, SetConcurrentJobLimitCommand>();
        JsonMessageTypeMappingRegistry.Register<ISetJobProgress, SetJobProgressCommand>();
        JsonMessageTypeMappingRegistry.Register<IStartJob, StartJobCommand>();
        JsonMessageTypeMappingRegistry.Register<IStartJobAttempt, StartJobAttemptCommand>();
    }
}
