using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;

namespace ViciOne.ServiceBus;

public static class AzureTableJobServiceConfigurationExtensions
{
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
