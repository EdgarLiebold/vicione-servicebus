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

        configurator.SendTopology.UsePartitionKeyFormatter<IAllocateJobSlot>(x => x.Message.JobTypeId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobSlotReleased>(x => x.Message.JobTypeId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<ISetConcurrentJobLimit>(x => x.Message.JobTypeId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<IJobSubmitted>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobSlotAllocated>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobSlotUnavailable>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<IAllocateJobSlot>>(x => x.Message.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<IStartJobAttempt>>(x => x.Message.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobAttemptCanceled>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobAttemptCompleted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobAttemptFaulted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobAttemptStarted>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobCompleted>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IGetJobState>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IStartJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<ICancelJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IRetryJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IRunJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<ISaveJobCheckpoint>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<ISetJobProgress>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobSlotWaitElapsed>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobRetryDelayElapsed>(x => x.Message.JobId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<IStartJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IFinalizeJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<ICancelJobAttempt>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<Fault<IStartJob>>(x => x.Message.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobAttemptStatus>(x => x.Message.AttemptId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IJobStatusCheckRequested>(x => x.Message.AttemptId.ToString("N"));

        configurator.SendTopology.UsePartitionKeyFormatter<ICompleteJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IFaultJob>(x => x.Message.JobId.ToString("N"));
        configurator.SendTopology.UsePartitionKeyFormatter<IGetJobAttemptStatus>(x => x.Message.JobId.ToString("N"));
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
