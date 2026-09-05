using System.Reflection;
using global::Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.MessageJournal;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.AzureTable.Configuration;

public sealed class AzureTableConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "database-and-provider-dependencies-fail-fast")]
    public void DatabaseAndProviderConstructors_RejectMissingDependenciesWithExactOwnership()
    {
        TableClient table = CreateTableClient();
        var formatter = new ConstPartitionSagaKeyFormatter<ConfigurationSaga>(nameof(ConfigurationSaga));
        var consumeFactory = new SagaConsumeContextFactory<DatabaseContext<ConfigurationSaga>, ConfigurationSaga>();
        var provider = new FixedTableClientProvider<ConfigurationSaga>(table);

        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableDatabaseContext<ConfigurationSaga>(null!, formatter)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableDatabaseContext<ConfigurationSaga>(table, null!)).ParamName);
        Assert.Equal("tableClient", Assert.Throws<ArgumentNullException>(() =>
            new FixedTableClientProvider<ConfigurationSaga>(null!)).ParamName);
        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            new DelegateTableClientProvider<ConfigurationSaga>(null!)).ParamName);
        Assert.Equal("tableClientProvider", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(
                (ITableClientProvider<ConfigurationSaga>)null!, consumeFactory, formatter)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(provider, null!, formatter)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(provider, consumeFactory, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "repository-factory-dependencies-fail-fast")]
    public void RepositoryCreation_RejectsMissingFactoryAndFormatterWithExactOwnership()
    {
        TableClient table = CreateTableClient();
        Func<TableClient> tableFactory = () => table;

        Assert.Equal("tableFactory", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepository<ConfigurationSaga>.Create((Func<TableClient>)null!)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepository<ConfigurationSaga>.Create(tableFactory, null!)).ParamName);
        Assert.IsAssignableFrom<ISagaRepository<ConfigurationSaga>>(
            AzureTableSagaRepository<ConfigurationSaga>.Create(tableFactory));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "job-service-assigns-all-three-repositories-and-fails-fast")]
    public void JobServiceConfiguration_AssignsAllRepositoriesAndRejectsMissingDependencies()
    {
        TableClient table = CreateTableClient();
        Func<TableClient> tableFactory = () => table;
        var jobTypeFormatter = new ConstPartitionSagaKeyFormatter<JobTypeSaga>(nameof(JobTypeSaga));
        var jobFormatter = new ConstPartitionSagaKeyFormatter<JobSaga>(nameof(JobSaga));
        var attemptFormatter = new ConstPartitionSagaKeyFormatter<JobAttemptSaga>(nameof(JobAttemptSaga));
        var configurator = new RecordingJobServiceConfigurator();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableJobServiceConfigurationExtensions.UseAzureTableSagaRepository(
                null!, tableFactory, jobTypeFormatter, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("contextFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTableSagaRepository(null!, jobTypeFormatter, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("jobTypeKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTableSagaRepository(tableFactory, null!, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("jobKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTableSagaRepository(tableFactory, jobTypeFormatter, null!, attemptFormatter)).ParamName);
        Assert.Equal("jobAttemptKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTableSagaRepository(tableFactory, jobTypeFormatter, jobFormatter, null!)).ParamName);

        configurator.UseAzureTableSagaRepository(
            tableFactory,
            jobTypeFormatter,
            jobFormatter,
            attemptFormatter);

        Assert.NotNull(configurator.Repository);
        Assert.NotNull(configurator.JobRepository);
        Assert.NotNull(configurator.JobAttemptRepository);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "registration-provider-rejects-null-configuration")]
    public void RegistrationProvider_RejectsANullConfigurationDelegate()
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryRegistrationProvider(null!));

        Assert.Equal("configure", failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "public-composition-entry-points-fail-fast")]
    public void PublicCompositionEntryPoints_RejectMissingOwnersBeforeDeferredExecution()
    {
        var configurator = new AzureTableSagaRepositoryConfigurator<ConfigurationSaga>();
        var provider = new FixedTableClientProvider<ConfigurationSaga>(CreateTableClient());
        var contextFactory = new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(
            provider,
            new SagaConsumeContextFactory<DatabaseContext<ConfigurationSaga>, ConfigurationSaga>(),
            new ConstPartitionSagaKeyFormatter<ConfigurationSaga>(nameof(ConfigurationSaga)));

        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.TableClientFactory((Func<TableClient>)null!)).ParamName);
        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.TableClientFactory((Func<IServiceProvider, TableClient>)null!)).ParamName);
        Assert.Equal("formatterFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.KeyFormatter(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            configurator.Register(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableRepositoryRegistrationExtensions.AzureTableRepository<ConfigurationSaga>(null!, null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableRepositoryRegistrationExtensions.AzureTableRepository(
                (IJobSagaRegistrationConfigurator)null!,
                static _ => { })).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableRepositoryRegistrationExtensions.SetAzureTableSagaRepositoryProvider(
                null!,
                static _ => { })).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableMessageJournalConfigurationExtensions.UseAzureTableMessageJournal(
                null!,
                null!,
                null!,
                null!,
                null!,
                null!)).ParamName);
        Assert.Equal("asyncMethod", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = contextFactory.ExecuteAsync<ConfigurationSaga>(null!, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            contextFactory.Probe(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "public-api-uses-table-client-vocabulary")]
    public void PublicApi_UsesTableClientVocabularyWithoutLegacyCloudTableAliases()
    {
        Type contract = typeof(IAzureTableSagaRepositoryConfigurator);
        MethodInfo[] factories = contract.GetMethods()
            .Where(method => method.Name == nameof(IAzureTableSagaRepositoryConfigurator.TableClientFactory))
            .ToArray();

        Assert.Equal(2, factories.Length);
        Assert.Contains(factories, method =>
            method.GetParameters().Single().ParameterType == typeof(Func<TableClient>));
        Assert.Contains(factories, method =>
            method.GetParameters().Single().ParameterType == typeof(Func<IServiceProvider, TableClient>));
        Assert.DoesNotContain(contract.GetMethods(), method => method.Name == "ConnectionFactory");
        Assert.DoesNotContain(
            contract.Assembly.GetExportedTypes(),
            type => type.Name.Contains("CloudTable", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "bus-block-journal-provider-selection-does-no-network-io")]
    public void BusBlockJournalProviderSelection_CreatesTheBoundedStoreWithoutNetworkIo()
    {
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));
        var options = new AzureTableMessageJournalStoreOptions("journal", limits);
        var configurator = new RecordingJournalConfigurator();

        IMessageJournalConfigurator result = configurator.UseAzureTable(CreateTableClient(), options);

        Assert.Same(configurator, result);
        var store = Assert.IsType<AzureTableMessageJournalStore>(configurator.Store);
        Assert.Same(limits, store.Limits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "registration-resolves-internal-table-client-provider")]
    public void Registration_ResolvesBothRepositoryContractsThroughTheInternalTableClientProvider()
    {
        TableClient tableClient = CreateTableClient();
        var services = new ServiceCollection();
        var registration = new SagaRepositoryRegistrationConfigurator<ConfigurationSaga>(services);
        var configurator = new AzureTableSagaRepositoryConfigurator<ConfigurationSaga>();
        configurator.TableClientFactory(() => tableClient);

        configurator.Register(registration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        ISagaRepositoryContextFactory<ConfigurationSaga> repositoryFactory = scope.ServiceProvider
            .GetRequiredService<ISagaRepositoryContextFactory<ConfigurationSaga>>();
        ILoadSagaRepositoryContextFactory<ConfigurationSaga> loadFactory = scope.ServiceProvider
            .GetRequiredService<ILoadSagaRepositoryContextFactory<ConfigurationSaga>>();

        Assert.IsType<AzureTableSagaRepositoryContextFactory<ConfigurationSaga>>(repositoryFactory);
        Assert.Same(repositoryFactory, loadFactory);
    }

    public sealed class ConfigurationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private static TableClient CreateTableClient()
    {
        var credential = new TableSharedKeyCredential("localaccount", Convert.ToBase64String(new byte[32]));
        return new TableClient(new Uri("http://127.0.0.1:1/localaccount"), "sagas", credential);
    }

    private sealed class RecordingJobServiceConfigurator : IJobServiceConfigurator
    {
        public ISagaRepository<JobTypeSaga>? Repository { get; set; }
        public ISagaRepository<JobSaga>? JobRepository { get; set; }
        public ISagaRepository<JobAttemptSaga>? JobAttemptRepository { get; set; }
        public string JobServiceStateEndpointName { set { } }
        public string JobServiceJobStateEndpointName { set { } }
        public string JobServiceJobAttemptStateEndpointName { set { } }
        public TimeSpan SlotWaitTime { set { } }
        public TimeSpan StatusCheckInterval { set { } }
        public int SuspectJobRetryCount { set { } }
        public TimeSpan SuspectJobRetryDelay { set { } }
        public int? SagaPartitionCount { set { } }
        public bool FinalizeCompleted { set { } }
        public Func<string, TimeZoneInfo> TimeZoneResolver { set { } }
    }

    private sealed class RecordingJournalConfigurator : IMessageJournalConfigurator
    {
        public IMessageJournalStore? Store { get; private set; }

        public IMessageJournalConfigurator UseStore(IMessageJournalStore store)
        {
            Store = store;
            return this;
        }

        public IMessageJournalConfigurator Policy(IMessageJournalPolicy policy) => this;

        public IMessageJournalConfigurator Options(MessageJournalOptions options) => this;
    }
}
