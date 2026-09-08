using System.Reflection;
using global::Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.Configuration;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.Configuration;

public sealed class AzureTableConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "database-and-provider-dependencies-fail-fast")]
    public void DatabaseAndProviderConstructors_RejectMissingDependenciesWithExactOwnership()
    {
        TableClient table = CreateTableClient();
        var formatter = new FixedPartitionSagaKeyFormatter(nameof(ConfigurationSaga));
        var consumeFactory = new SagaConsumeContextFactory<IAzureTableSagaStorageContext<ConfigurationSaga>, ConfigurationSaga>();
        var provider = new FixedAzureTableClientProvider<ConfigurationSaga>(table);

        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaStorageContext<ConfigurationSaga>(null!, formatter)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaStorageContext<ConfigurationSaga>(table, null!)).ParamName);
        Assert.Equal("tableClient", Assert.Throws<ArgumentNullException>(() =>
            new FixedAzureTableClientProvider<ConfigurationSaga>(null!)).ParamName);
        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            new DelegateAzureTableClientProvider<ConfigurationSaga>(null!)).ParamName);
        Assert.Equal("tableClientProvider", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(
                (IAzureTableClientProvider<ConfigurationSaga>)null!, consumeFactory, formatter)).ParamName);
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

        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepository.Create<ConfigurationSaga>((Func<TableClient>)null!)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepository.Create<ConfigurationSaga>(tableFactory, null!)).ParamName);
        Assert.IsAssignableFrom<ISagaRepository<ConfigurationSaga>>(
            AzureTableSagaRepository.Create<ConfigurationSaga>(tableFactory));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "job-service-assigns-all-three-repositories-and-fails-fast")]
    public void JobServiceConfiguration_AssignsAllRepositoriesAndRejectsMissingDependencies()
    {
        TableClient table = CreateTableClient();
        Func<TableClient> tableFactory = () => table;
        var jobTypeFormatter = new FixedPartitionSagaKeyFormatter(nameof(JobTypeSaga));
        var jobFormatter = new FixedPartitionSagaKeyFormatter(nameof(JobSaga));
        var attemptFormatter = new FixedPartitionSagaKeyFormatter(nameof(JobAttemptSaga));
        var configurator = new RecordingJobServiceConfigurator();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableJobServiceConfigurationExtensions.UseAzureTable(
                null!, tableFactory, jobTypeFormatter, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTable(null!, jobTypeFormatter, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("jobTypeKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTable(tableFactory, null!, jobFormatter, attemptFormatter)).ParamName);
        Assert.Equal("jobKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTable(tableFactory, jobTypeFormatter, null!, attemptFormatter)).ParamName);
        Assert.Equal("jobAttemptKeyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseAzureTable(tableFactory, jobTypeFormatter, jobFormatter, null!)).ParamName);

        configurator.UseAzureTable(
            tableFactory,
            jobTypeFormatter,
            jobFormatter,
            attemptFormatter);

        Assert.NotNull(configurator.JobTypeRepository);
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
        var provider = new FixedAzureTableClientProvider<ConfigurationSaga>(CreateTableClient());
        ISagaRegistrationConfigurator<ConfigurationSaga> sagaRegistration =
            DispatchProxy.Create<ISagaRegistrationConfigurator<ConfigurationSaga>, UnsupportedInvocationProxy>();
        IJobSagaRegistrationConfigurator jobRegistration =
            DispatchProxy.Create<IJobSagaRegistrationConfigurator, UnsupportedInvocationProxy>();
        IRegistrationConfigurator registration =
            DispatchProxy.Create<IRegistrationConfigurator, UnsupportedInvocationProxy>();
        var contextFactory = new AzureTableSagaRepositoryContextFactory<ConfigurationSaga>(
            provider,
            new SagaConsumeContextFactory<IAzureTableSagaStorageContext<ConfigurationSaga>, ConfigurationSaga>(),
            new FixedPartitionSagaKeyFormatter(nameof(ConfigurationSaga)));

        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseTableClientFactory((Func<TableClient>)null!)).ParamName);
        Assert.Equal("tableClientFactory", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseTableClientFactory((Func<IServiceProvider, TableClient>)null!)).ParamName);
        Assert.Equal("keyFormatter", Assert.Throws<ArgumentNullException>(() =>
            configurator.UseKeyFormatter(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            configurator.Register(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepositoryRegistrationExtensions.UseAzureTable<ConfigurationSaga>(null!, null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            sagaRegistration.UseAzureTable(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepositoryRegistrationExtensions.UseAzureTable(
                (IJobSagaRegistrationConfigurator)null!,
                static _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            jobRegistration.UseAzureTable(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableSagaRepositoryRegistrationExtensions.UseAzureTableForRegisteredSagas(
                null!,
                static _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            registration.UseAzureTableForRegisteredSagas(null!)).ParamName);
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
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-CONFIGURATION", "journal-composition-rejects-every-invalid-argument")]
    public void JournalComposition_RejectsEveryInvalidArgumentBeforeNetworkUse()
    {
        TableClient table = CreateTableClient();
        var credential = new TableSharedKeyCredential("localaccount", Convert.ToBase64String(new byte[32]));
        var serviceClient = new TableServiceClient(new Uri("http://127.0.0.1:1/localaccount"), credential);
        var storeOptions = new AzureTableMessageJournalStoreOptions(
            "journal",
            new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2)));
        var journalConfigurator = new RecordingJournalConfigurator();
        IBusFactoryConfigurator busConfigurator =
            DispatchProxy.Create<IBusFactoryConfigurator, UnsupportedInvocationProxy>();
        IMessageJournalPolicy policy =
            DispatchProxy.Create<IMessageJournalPolicy, UnsupportedInvocationProxy>();
        MessageJournalOptions journalOptions = MessageJournalOptions.ContinueMessageFlow(
            TimeSpan.FromSeconds(5),
            TimeProvider.System);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureTableMessageJournalConfigurationExtensions.UseAzureTable(null!, table, storeOptions)).ParamName);
        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() =>
            journalConfigurator.UseAzureTable((TableClient)null!, storeOptions)).ParamName);
        Assert.Equal("storeOptions", Assert.Throws<ArgumentNullException>(() =>
            journalConfigurator.UseAzureTable(table, null!)).ParamName);
        Assert.Equal("tableServiceClient", Assert.Throws<ArgumentNullException>(() =>
            journalConfigurator.UseAzureTable((TableServiceClient)null!, "journal", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, " ", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "ta", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, new string('a', 64), storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "1table", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "table-name", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "täble", storeOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "tables", storeOptions)).ParamName);
        Assert.Equal("storeOptions", Assert.Throws<ArgumentNullException>(() =>
            journalConfigurator.UseAzureTable(serviceClient, "journal", null!)).ParamName);

        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(null!, storeOptions, policy, journalOptions)).ParamName);
        Assert.Equal("storeOptions", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(table, null!, policy, journalOptions)).ParamName);
        Assert.Equal("policy", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(table, storeOptions, null!, journalOptions)).ParamName);
        Assert.Equal("journalOptions", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(table, storeOptions, policy, null!)).ParamName);
        Assert.Equal("tableServiceClient", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(null!, "journal", storeOptions, policy, journalOptions)).ParamName);
        Assert.Equal("tableName", Assert.Throws<ArgumentException>(() =>
            busConfigurator.UseAzureTableMessageJournal(serviceClient, "", storeOptions, policy, journalOptions)).ParamName);
        Assert.Equal("storeOptions", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(serviceClient, "journal", null!, policy, journalOptions)).ParamName);
        Assert.Equal("policy", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(serviceClient, "journal", storeOptions, null!, journalOptions)).ParamName);
        Assert.Equal("journalOptions", Assert.Throws<ArgumentNullException>(() =>
            busConfigurator.UseAzureTableMessageJournal(serviceClient, "journal", storeOptions, policy, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-PUBLIC-API", "minimal-greenfield-surface-and-table-client-vocabulary")]
    public void PublicApi_IsMinimalAndUsesTableClientVocabulary()
    {
        string[] exportedTypes =
        [
            .. typeof(AzureTableSagaRepository).Assembly
                .GetExportedTypes()
                .Select(type => type.FullName!)
                .Order(StringComparer.Ordinal),
        ];
        Assert.Equal(
            [
                "ViciOne.ServiceBus.Azure.Table.AzureTableJobServiceConfigurationExtensions",
                "ViciOne.ServiceBus.Azure.Table.AzureTableMessageJournalConfigurationExtensions",
                "ViciOne.ServiceBus.Azure.Table.AzureTableMessageJournalStore",
                "ViciOne.ServiceBus.Azure.Table.AzureTableMessageJournalStoreOptions",
                "ViciOne.ServiceBus.Azure.Table.AzureTableSagaConcurrencyException",
                "ViciOne.ServiceBus.Azure.Table.AzureTableSagaRepository",
                "ViciOne.ServiceBus.Azure.Table.AzureTableSagaRepositoryRegistrationExtensions",
                "ViciOne.ServiceBus.Azure.Table.FixedPartitionSagaKeyFormatter",
                "ViciOne.ServiceBus.Azure.Table.FixedRowSagaKeyFormatter",
                "ViciOne.ServiceBus.Azure.Table.IAzureTableSagaKeyFormatter",
                "ViciOne.ServiceBus.Azure.Table.IAzureTableSagaRepositoryConfigurator",
            ],
            exportedTypes);

        Type contract = typeof(IAzureTableSagaRepositoryConfigurator);
        MethodInfo[] factories = contract.GetMethods()
            .Where(method => method.Name == nameof(IAzureTableSagaRepositoryConfigurator.UseTableClientFactory))
            .ToArray();

        Assert.Equal(2, factories.Length);
        Assert.Contains(factories, method =>
            method.GetParameters().Single().ParameterType == typeof(Func<TableClient>));
        Assert.Contains(factories, method =>
            method.GetParameters().Single().ParameterType == typeof(Func<IServiceProvider, TableClient>));
        Assert.Equal(
            typeof(IAzureTableSagaKeyFormatter),
            contract.GetMethod(nameof(IAzureTableSagaRepositoryConfigurator.UseKeyFormatter))!
                .GetParameters()
                .Single()
                .ParameterType);
        Assert.DoesNotContain(contract.GetMethods(), method => method.Name == "ConnectionFactory");
        Assert.DoesNotContain(
            contract.Assembly.GetExportedTypes(),
            type => type.Name.Contains("CloudTable", StringComparison.Ordinal));

        Assert.Equal(
            ["UseAzureTable", "UseAzureTable", "UseAzureTableForRegisteredSagas"],
            typeof(AzureTableSagaRepositoryRegistrationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["UseAzureTable", "UseAzureTable"],
            typeof(AzureTableJobServiceConfigurationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["UseAzureTable", "UseAzureTable", "UseAzureTableMessageJournal", "UseAzureTableMessageJournal"],
            typeof(AzureTableMessageJournalConfigurationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["Create", "Create"],
            typeof(AzureTableSagaRepository)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
        Assert.All(
            typeof(AzureTableSagaRepository).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => Assert.True(method.IsGenericMethodDefinition));
        Assert.True(typeof(AzureTableSagaConcurrencyException).IsSealed);
        Assert.True(typeof(FixedPartitionSagaKeyFormatter).IsSealed);
        Assert.True(typeof(FixedRowSagaKeyFormatter).IsSealed);

        Guid correlationId = Guid.Parse("018cc251-f400-7000-8000-000000000501");
        var innerException = new InvalidOperationException("etag conflict");
        var concurrency = new AzureTableSagaConcurrencyException(
            "The persisted saga changed.",
            typeof(ConfigurationSaga),
            correlationId,
            innerException);
        Assert.Equal(typeof(ConfigurationSaga), concurrency.SagaType);
        Assert.Equal(correlationId, concurrency.CorrelationId);
        Assert.Same(innerException, concurrency.InnerException);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaConcurrencyException(null!, typeof(ConfigurationSaga), correlationId)).ParamName);
        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaConcurrencyException("conflict", null!, correlationId)).ParamName);
        Assert.Equal("correlationId", Assert.Throws<ArgumentException>(() =>
            new AzureTableSagaConcurrencyException("conflict", typeof(ConfigurationSaga), Guid.Empty)).ParamName);
        Assert.Equal("innerException", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableSagaConcurrencyException(
                "conflict",
                typeof(ConfigurationSaga),
                correlationId,
                null!)).ParamName);
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
        var marker = new ServiceProviderMarker();
        var formatter = new FixedRowSagaKeyFormatter("state");
        var secondaryFormatter = new FixedPartitionSagaKeyFormatter("secondary");
        IServiceProvider? observedServiceProvider = null;
        var services = new ServiceCollection();
        services.AddSingleton(marker);
        var registration = new SagaRepositoryRegistrationConfigurator<ConfigurationSaga>(services);
        var secondaryRegistration = new SagaRepositoryRegistrationConfigurator<SecondaryConfigurationSaga>(services);
        var configurator = new AzureTableSagaRepositoryConfigurator<ConfigurationSaga>();
        var secondaryConfigurator = new AzureTableSagaRepositoryConfigurator<SecondaryConfigurationSaga>();
        configurator.UseTableClientFactory(serviceProvider =>
        {
            observedServiceProvider = serviceProvider;
            Assert.Same(marker, serviceProvider.GetRequiredService<ServiceProviderMarker>());
            return tableClient;
        });
        configurator.UseKeyFormatter(formatter);
        secondaryConfigurator.UseTableClientFactory(() => tableClient);
        secondaryConfigurator.UseKeyFormatter(secondaryFormatter);

        configurator.Register(registration);
        secondaryConfigurator.Register(secondaryRegistration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        ISagaRepositoryContextFactory<ConfigurationSaga> repositoryFactory = scope.ServiceProvider
            .GetRequiredService<ISagaRepositoryContextFactory<ConfigurationSaga>>();
        ILoadSagaRepositoryContextFactory<ConfigurationSaga> loadFactory = scope.ServiceProvider
            .GetRequiredService<ILoadSagaRepositoryContextFactory<ConfigurationSaga>>();
        IAzureTableClientProvider<ConfigurationSaga> tableClientProvider = scope.ServiceProvider
            .GetRequiredService<IAzureTableClientProvider<ConfigurationSaga>>();
        AzureTableSagaKeyFormatterProvider<ConfigurationSaga> keyFormatterProvider = scope.ServiceProvider
            .GetRequiredService<AzureTableSagaKeyFormatterProvider<ConfigurationSaga>>();
        AzureTableSagaKeyFormatterProvider<SecondaryConfigurationSaga> secondaryKeyFormatterProvider = scope.ServiceProvider
            .GetRequiredService<AzureTableSagaKeyFormatterProvider<SecondaryConfigurationSaga>>();

        Assert.IsType<AzureTableSagaRepositoryContextFactory<ConfigurationSaga>>(repositoryFactory);
        Assert.Same(repositoryFactory, loadFactory);
        Assert.NotNull(observedServiceProvider);
        Assert.Same(tableClient, tableClientProvider.GetTableClient());
        Assert.Same(formatter, keyFormatterProvider.Formatter);
        Assert.Same(secondaryFormatter, secondaryKeyFormatterProvider.Formatter);
        Assert.NotSame(keyFormatterProvider, secondaryKeyFormatterProvider);
    }

    public sealed class ConfigurationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class SecondaryConfigurationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class ServiceProviderMarker
    {
    }

    private static TableClient CreateTableClient()
    {
        var credential = new TableSharedKeyCredential("localaccount", Convert.ToBase64String(new byte[32]));
        return new TableClient(new Uri("http://127.0.0.1:1/localaccount"), "sagas", credential);
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

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
