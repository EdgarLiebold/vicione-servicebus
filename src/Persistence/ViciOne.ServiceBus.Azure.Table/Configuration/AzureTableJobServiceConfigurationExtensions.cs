using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Configures Job Service saga state to use Azure Table Storage.</summary>
public static class AzureTableJobServiceConfigurationExtensions
{
    /// <summary>Assigns Azure Table repositories and explicit key formats to all three Job Service saga types.</summary>
    /// <param name="configurator">The Job Service configurator to update.</param>
    /// <param name="contextFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <param name="jobTypeKeyFormatter">The key strategy for job-type saga entities.</param>
    /// <param name="jobKeyFormatter">The key strategy for job saga entities.</param>
    /// <param name="jobAttemptKeyFormatter">The key strategy for job-attempt saga entities.</param>
    public static void UseAzureTableSagaRepository(this IJobServiceConfigurator configurator,
        Func<TableClient> contextFactory,
        ISagaKeyFormatter<JobTypeSaga> jobTypeKeyFormatter,
        ISagaKeyFormatter<JobSaga> jobKeyFormatter,
        ISagaKeyFormatter<JobAttemptSaga> jobAttemptKeyFormatter)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(jobTypeKeyFormatter);
        ArgumentNullException.ThrowIfNull(jobKeyFormatter);
        ArgumentNullException.ThrowIfNull(jobAttemptKeyFormatter);

        configurator.JobTypeRepository = AzureTableSagaRepository<JobTypeSaga>.Create(contextFactory, jobTypeKeyFormatter);

        configurator.JobRepository = AzureTableSagaRepository<JobSaga>.Create(contextFactory, jobKeyFormatter);

        configurator.JobAttemptRepository = AzureTableSagaRepository<JobAttemptSaga>.Create(contextFactory, jobAttemptKeyFormatter);
    }

    /// <summary>Assigns Azure Table repositories that partition each Job Service saga type by its type name.</summary>
    /// <param name="configurator">The Job Service configurator to update.</param>
    /// <param name="contextFactory">The factory that supplies the Azure Table client for each repository context.</param>
    public static void UseAzureTableSagaRepository(this IJobServiceConfigurator configurator,
        Func<TableClient> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextFactory);

        UseAzureTableSagaRepository(configurator, contextFactory,
            new ConstPartitionSagaKeyFormatter<JobTypeSaga>(nameof(JobTypeSaga)),
            new ConstPartitionSagaKeyFormatter<JobSaga>(nameof(JobSaga)),
            new ConstPartitionSagaKeyFormatter<JobAttemptSaga>(nameof(JobAttemptSaga)));
    }
}
