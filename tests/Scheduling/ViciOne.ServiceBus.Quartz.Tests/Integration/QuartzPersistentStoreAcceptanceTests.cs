using Microsoft.Data.Sqlite;
using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzPersistentStoreAcceptanceTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly Uri Destination = new("loopback://quartz-persistent/persistent-target");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PERSISTENCE", "sqlite-store-survives-factory-and-bus-restart")]
    public async Task SqliteStore_RestoresTheExactTriggerAndDeliversAfterACompleteRestartAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"vicione-quartz-{NewId.NextGuid():N}.db");
        string connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
        const string schedulerName = "ViciOne.ServiceBus.Quartz.PersistenceAcceptance";
        const string queueName = "quartz-persistent-commands";
        string schedulerNamespace = QuartzSchedulerNamespace.ForEndpoint(queueName);
        RetryPolicy retryPolicy = RetryPolicy.Explicit([
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(11),
        ]);
        ISchedulerFactory? firstFactory = null;
        ISchedulerFactory? secondFactory = null;
        IBusControl? firstBus = null;
        IBusControl? secondBus = null;
        QuartzSchedulerLease? firstLease = null;
        QuartzSchedulerLease? secondLease = null;

        SQLitePCL.Batteries_V2.Init();
        try
        {
            firstFactory = CreatePersistentFactory(schedulerName, connectionString);
            ISchedulerFactory schedulingFactory = firstFactory;
            firstBus = Bus.Factory.CreateUsingInMemory(configurator =>
            {
                configurator.Host(new Uri("loopback://quartz-persistent/"));
                firstLease = configurator.ConfigureQuartzScheduler(schedulingFactory, options =>
                {
                    options.QueueName = queueName;
                    options.DeliveryRetryPolicy = retryPolicy;
                });
            });
            await firstBus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            using (firstBus.ConnectConsumeObserver(scheduledCommand))
            {
                QuartzSchedulerLease configuredLease = Assert.IsType<QuartzSchedulerLease>(firstLease);
                ISendEndpoint schedulerEndpoint = await firstBus.GetSendEndpointAsync(
                        configuredLease.EndpointAddress,
                        cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
                var messageScheduler = new MessageScheduler(
                    new EndpointScheduleMessageProvider(_ => Task.FromResult(schedulerEndpoint)),
                    firstBus.Topology);

                ScheduledMessage<PersistedPayload> scheduled = await messageScheduler.ScheduleSendAsync(
                    Destination,
                    DueAt,
                    new PersistedPayload("survived"),
                    Pipe.Execute<SendContext<PersistedPayload>>(context => context.Headers.Set("tenant", "north")),
                    cancellationToken);
                await scheduledCommand.Completed.WaitAsync(timeout, cancellationToken);

                IScheduler firstScheduler = await firstFactory.GetScheduler(cancellationToken)
                    .AsTask().WaitAsync(timeout, cancellationToken);
                TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(scheduled.TokenId, schedulerNamespace);
                ITrigger firstTrigger = Assert.IsAssignableFrom<ITrigger>(
                    await firstScheduler.GetTrigger(triggerKey, cancellationToken));
                Assert.Equal(retryPolicy, firstTrigger.RetryPolicy);
                string messageIdSeed = Assert.IsType<string>(firstTrigger.JobDataMap[QuartzJobDataKeys.MessageIdSeed]);
                Assert.True(Guid.TryParseExact(messageIdSeed, "D", out _));

                await firstBus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
                firstBus = null;
                await configuredLease.DisposeAsync();
                firstLease = null;
                await DisposeFactoryAsync(firstFactory);
                firstFactory = null;

                var delivered = new TaskCompletionSource<PersistedDelivery>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                secondFactory = CreatePersistentFactory(schedulerName, connectionString);
                ISchedulerFactory restoredFactory = secondFactory;
                secondBus = Bus.Factory.CreateUsingInMemory(configurator =>
                {
                    configurator.Host(new Uri("loopback://quartz-persistent/"));
                    configurator.ReceiveEndpoint("persistent-target", endpoint =>
                        endpoint.Handler<PersistedPayload>(context =>
                        {
                            delivered.TrySetResult(new PersistedDelivery(
                                context.Message.Value,
                                context.Headers.Get<string>("tenant")));
                            return Task.CompletedTask;
                        }));
                    secondLease = configurator.ConfigureQuartzScheduler(restoredFactory, options =>
                    {
                        options.QueueName = queueName;
                        options.DeliveryRetryPolicy = retryPolicy;
                    });
                });
                await secondBus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
                IScheduler secondScheduler = await secondFactory.GetScheduler(cancellationToken)
                    .AsTask().WaitAsync(timeout, cancellationToken);
                ITrigger restoredTrigger = Assert.IsAssignableFrom<ITrigger>(
                    await secondScheduler.GetTrigger(triggerKey, cancellationToken));
                IJobDetail restoredJob = Assert.IsAssignableFrom<IJobDetail>(
                    await secondScheduler.GetJobDetail(restoredTrigger.JobKey, cancellationToken));

                Assert.Equal(typeof(QuartzScheduledMessageJob<IBus>), restoredJob.JobType);
                Assert.True(restoredJob.Durable);
                Assert.True(restoredJob.RequestsRecovery);
                Assert.Equal(DueAt, restoredTrigger.StartTimeUtc);
                Assert.Equal(retryPolicy, restoredTrigger.RetryPolicy);
                Assert.Equal(0, restoredTrigger.RetryAttempt);
                Assert.Equal(messageIdSeed, restoredTrigger.JobDataMap.GetString(QuartzJobDataKeys.MessageIdSeed));

                await secondScheduler.TriggerJob(
                    restoredTrigger.JobKey,
                    restoredTrigger.JobDataMap,
                    cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
                PersistedDelivery received = await delivered.Task.WaitAsync(timeout, cancellationToken);
                Assert.Equal(new PersistedDelivery("survived", "north"), received);
            }
        }
        finally
        {
            if (secondBus is not null)
                await secondBus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (firstBus is not null)
                await firstBus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (secondLease is not null)
                await secondLease.DisposeAsync();
            if (firstLease is not null)
                await firstLease.DisposeAsync();
            if (secondFactory is not null)
                await DisposeFactoryAsync(secondFactory);
            if (firstFactory is not null)
                await DisposeFactoryAsync(firstFactory);
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static ISchedulerFactory CreatePersistentFactory(string schedulerName, string connectionString)
    {
        return QuartzSchedulerBuilder.Create(configuration => configuration
                .ConfigureScheduler(options => options.InstanceName = schedulerName)
                .UseDefaultThreadPool(1)
                .UsePersistentStore(store =>
                {
                    store.UseSqlite(SqliteFactory.Instance, connectionString);
                    store.ConfigureStore(options => options.StoreJobDataAsStrings = true);
                    store.ProvisionSchema();
                }))
            .Build();
    }

    private static async ValueTask DisposeFactoryAsync(ISchedulerFactory factory)
    {
        foreach (IScheduler scheduler in await factory.GetAllSchedulers())
        {
            if (scheduler.Status != SchedulerStatus.Shutdown)
                await scheduler.Shutdown(waitForJobsToComplete: true);
        }

        if (factory is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (factory is IDisposable disposable)
            disposable.Dispose();
    }

    public sealed record PersistedPayload(string Value);

    private sealed record PersistedDelivery(string Value, string? Tenant);
}
