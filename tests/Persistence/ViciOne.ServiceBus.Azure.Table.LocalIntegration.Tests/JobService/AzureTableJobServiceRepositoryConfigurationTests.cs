using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.JobService;

public sealed class AzureTableJobServiceRepositoryConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-JOB-SERVICE-REPOSITORIES", "public-overloads-load-three-saga-types-from-their-own-keys")]
    public async Task PublicOverloads_LoadEachJobSagaFromItsOwnPartitionAsync(bool customKeys)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync(
            "JobRepositoryKeys", cancellationToken);
        string typePartition = customKeys ? "custom-job-type" : nameof(JobTypeSaga);
        string jobPartition = customKeys ? "custom-job" : nameof(JobSaga);
        string attemptPartition = customKeys ? "custom-attempt" : nameof(JobAttemptSaga);
        Guid typeId = Guid.CreateVersion7();
        Guid jobId = Guid.CreateVersion7();
        Guid attemptId = Guid.CreateVersion7();
        var jobType = new JobTypeSaga { CorrelationId = typeId, Name = "type-marker" };
        var job = new JobSaga
        {
            CorrelationId = jobId,
            Reason = "job-marker",
            ServiceAddress = new Uri("loopback://localhost/job-service"),
        };
        var attempt = new JobAttemptSaga
        {
            CorrelationId = attemptId,
            JobId = jobId,
            RetryAttempt = 7,
            ServiceAddress = new Uri("loopback://localhost/job-service"),
            InstanceAddress = new Uri("loopback://localhost/job-instance"),
        };
        await SeedAsync(fixture.Table, jobType, typePartition, cancellationToken);
        await SeedAsync(fixture.Table, job, jobPartition, cancellationToken);
        await SeedAsync(fixture.Table, attempt, attemptPartition, cancellationToken);

        var configurator = new RecordingJobServiceConfigurator();
        if (customKeys)
        {
            configurator.UseAzureTable(
                () => fixture.Table,
                new FixedPartitionSagaKeyFormatter(typePartition),
                new FixedPartitionSagaKeyFormatter(jobPartition),
                new FixedPartitionSagaKeyFormatter(attemptPartition));
        }
        else
        {
            configurator.UseAzureTable(() => fixture.Table);
        }

        JobTypeSaga loadedType = Assert.IsType<JobTypeSaga>(await Assert.IsAssignableFrom<ILoadSagaRepository<JobTypeSaga>>(
            configurator.JobTypeRepository).LoadAsync(typeId, cancellationToken));
        JobSaga loadedJob = Assert.IsType<JobSaga>(await Assert.IsAssignableFrom<ILoadSagaRepository<JobSaga>>(
            configurator.JobRepository).LoadAsync(jobId, cancellationToken));
        JobAttemptSaga loadedAttempt = Assert.IsType<JobAttemptSaga>(await Assert.IsAssignableFrom<ILoadSagaRepository<JobAttemptSaga>>(
            configurator.JobAttemptRepository).LoadAsync(attemptId, cancellationToken));

        Assert.Equal(typeId, loadedType.CorrelationId);
        Assert.Equal("type-marker", loadedType.Name);
        Assert.Equal(jobId, loadedJob.CorrelationId);
        Assert.Equal("job-marker", loadedJob.Reason);
        Assert.Equal(attemptId, loadedAttempt.CorrelationId);
        Assert.Equal(jobId, loadedAttempt.JobId);
        Assert.Equal(7, loadedAttempt.RetryAttempt);
    }

    private static async Task SeedAsync<TSaga>(
        TableClient table,
        TSaga saga,
        string partition,
        CancellationToken cancellationToken)
        where TSaga : class, ISaga
    {
        var storage = new AzureTableSagaStorageContext<TSaga>(
            table, new FixedPartitionSagaKeyFormatter(partition));
        var entity = new TableEntity(storage.Converter.GetDictionary(saga));
        entity.PartitionKey = partition;
        entity.RowKey = saga.CorrelationId.ToString("D");
        await table.AddEntityAsync(entity, cancellationToken);
    }

    private sealed class RecordingJobServiceConfigurator : IJobServiceConfigurator
    {
        public ISagaRepository<JobTypeSaga>? JobTypeRepository { get; set; }
        public ISagaRepository<JobSaga>? JobRepository { get; set; }
        public ISagaRepository<JobAttemptSaga>? JobAttemptRepository { get; set; }
        public string JobTypeEndpointName { set { } }
        public string JobEndpointName { set { } }
        public string JobAttemptEndpointName { set { } }
        public TimeSpan HeartbeatInterval { set { } }
        public TimeSpan HeartbeatTimeout { set { } }
        public TimeSpan RejectedJobDelay { set { } }
        public TimeProvider TimeProvider { set { } }
        public TimeSpan SlotWaitTime { set { } }
        public TimeSpan StatusCheckInterval { set { } }
        public int SuspectJobRetryCount { set { } }
        public TimeSpan? SuspectJobRetryDelay { set { } }
        public int? ConcurrentMessageLimit { set { } }
        public bool FinalizeCompleted { set { } }
        public Func<string, TimeZoneInfo?>? TimeZoneResolver { set { } }
    }
}
