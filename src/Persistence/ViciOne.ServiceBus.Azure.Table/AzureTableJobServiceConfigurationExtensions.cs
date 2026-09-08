using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Configures Job Service saga state to use Azure Table Storage.</summary>
public static class AzureTableJobServiceConfigurationExtensions
{
    /// <summary>Assigns Azure Table repositories and explicit key formats to all three Job Service saga types.</summary>
    /// <param name="configurator">The Job Service configurator to update.</param>
    /// <param name="tableClientFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <param name="jobTypeKeyFormatter">The key strategy for job-type saga entities.</param>
    /// <param name="jobKeyFormatter">The key strategy for job saga entities.</param>
    /// <param name="jobAttemptKeyFormatter">The key strategy for job-attempt saga entities.</param>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    public static void UseAzureTable(this IJobServiceConfigurator configurator,
        Func<TableClient> tableClientFactory,
        IAzureTableSagaKeyFormatter jobTypeKeyFormatter,
        IAzureTableSagaKeyFormatter jobKeyFormatter,
        IAzureTableSagaKeyFormatter jobAttemptKeyFormatter)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        ArgumentNullException.ThrowIfNull(jobTypeKeyFormatter);
        ArgumentNullException.ThrowIfNull(jobKeyFormatter);
        ArgumentNullException.ThrowIfNull(jobAttemptKeyFormatter);

        configurator.JobTypeRepository = AzureTableSagaRepository.Create<JobTypeSaga>(tableClientFactory, jobTypeKeyFormatter);

        configurator.JobRepository = AzureTableSagaRepository.Create<JobSaga>(tableClientFactory, jobKeyFormatter);

        configurator.JobAttemptRepository = AzureTableSagaRepository.Create<JobAttemptSaga>(tableClientFactory, jobAttemptKeyFormatter);
    }

    /// <summary>Assigns Azure Table repositories that partition each Job Service saga type by its type name.</summary>
    /// <param name="configurator">The Job Service configurator to update.</param>
    /// <param name="tableClientFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="tableClientFactory"/> is <see langword="null"/>.</exception>
    public static void UseAzureTable(this IJobServiceConfigurator configurator,
        Func<TableClient> tableClientFactory)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(tableClientFactory);

        UseAzureTable(configurator, tableClientFactory,
            new FixedPartitionSagaKeyFormatter(nameof(JobTypeSaga)),
            new FixedPartitionSagaKeyFormatter(nameof(JobSaga)),
            new FixedPartitionSagaKeyFormatter(nameof(JobAttemptSaga)));
    }
}
