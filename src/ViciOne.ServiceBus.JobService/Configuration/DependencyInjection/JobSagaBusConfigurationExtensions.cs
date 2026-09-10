using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transport partition keys required by distributed job coordination.</summary>
public static class JobSagaBusConfigurationExtensions
{
    /// <summary>Adds stable partition keys for every command and event handled by job coordination endpoints.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void UseJobSagaPartitionKeyFormatters(this IBusFactoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SendTopology.UsePartitionKeyFormatter<AllocateJobSlot>(x => x.Message.JobTypeId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobSlotReleased>(x => x.Message.JobTypeId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<SetConcurrentJobLimit>(x => x.Message.JobTypeId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<JobSubmitted>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobSlotAllocated>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobSlotUnavailable>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<AllocateJobSlot>>(x => x.Message.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<StartJobAttempt>>(x => x.Message.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobAttemptCanceled>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobAttemptCompleted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobAttemptFaulted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobAttemptStarted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobCompleted>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<GetJobState>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<StartJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<CancelJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<RetryJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<RunJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<SaveJobCheckpoint>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<SetJobProgress>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobSlotWaitElapsed>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobRetryDelayElapsed>(x => x.Message.JobId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<StartJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<FinalizeJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<CancelJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<StartJob>>(x => x.Message.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobAttemptStatus>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<JobStatusCheckRequested>(x => x.Message.AttemptId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<CompleteJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<FaultJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<GetJobAttemptStatus>(x => x.Message.JobId.ToString("N"));
    }

    /// <summary>Configures every job coordination endpoint to process independent partition-key streams.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The supplied job coordination configurator.</returns>
    public static IJobSagaRegistrationConfigurator UsePartitionedReceiveMode(this IJobSagaRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator.ConfigureEndpoints(e =>
        {
            e.AddConfigureEndpointCallback(cfg =>
            {
                if (cfg is not IPartitionedReceiveEndpointConfigurator partitionedConfigurator)
                {
                    throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Job service",
                        cfg.InputAddress.ToString(),
                        $"The receive endpoint configurator '{cfg.GetType().FullName}' does not support partitioned receive processing.",
                        "Use a transport that supports partitioned receive processing, or omit UsePartitionedReceiveMode."));
                }

                partitionedConfigurator.SetPartitionedReceive();
            });
        });
    }
}
