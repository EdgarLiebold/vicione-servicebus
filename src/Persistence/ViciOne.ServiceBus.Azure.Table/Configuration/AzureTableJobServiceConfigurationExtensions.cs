using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>
/// Provides extension methods for azure table job service configuration.
/// </summary>
public static class AzureTableJobServiceConfigurationExtensions
{
    /// <summary>
    /// Configures azure table saga repository for the current pipeline.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="contextFactory">The context factory value.</param>
    /// <param name="jobTypeKeyFormatter">The job type key formatter value.</param>
    /// <param name="jobKeyFormatter">The job key formatter value.</param>
    /// <param name="jobAttemptKeyFormatter">The job attempt key formatter value.</param>
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

        configurator.Repository = AzureTableSagaRepository<JobTypeSaga>.Create(contextFactory, jobTypeKeyFormatter);

        configurator.JobRepository = AzureTableSagaRepository<JobSaga>.Create(contextFactory, jobKeyFormatter);

        configurator.JobAttemptRepository = AzureTableSagaRepository<JobAttemptSaga>.Create(contextFactory, jobAttemptKeyFormatter);
    }

    /// <summary>
    /// Configures azure table saga repository for the current pipeline.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="contextFactory">The context factory value.</param>
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
