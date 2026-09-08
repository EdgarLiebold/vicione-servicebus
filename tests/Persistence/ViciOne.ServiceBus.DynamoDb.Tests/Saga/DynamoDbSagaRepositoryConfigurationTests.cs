using System.Reflection;
using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDb;
using ViciOne.ServiceBus.DynamoDb.Configuration;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.Tests.Saga;

public sealed class DynamoDbSagaRepositoryConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-API", "minimal-greenfield-surface-and-null-boundaries")]
    public void PublicApi_IsMinimalAndRejectsInvalidArguments()
    {
        string[] exportedTypes =
        [
            .. typeof(DynamoDbSagaRepository).Assembly
                .GetExportedTypes()
                .Select(type => type.FullName!)
                .Order(StringComparer.Ordinal),
        ];
        Assert.Equal(
            [
                "ViciOne.ServiceBus.DynamoDb.DynamoDbSagaConcurrencyException",
                "ViciOne.ServiceBus.DynamoDb.DynamoDbSagaRepository",
                "ViciOne.ServiceBus.DynamoDb.DynamoDbSagaRepositoryOptions`1",
                "ViciOne.ServiceBus.DynamoDb.DynamoDbSagaRepositoryRegistrationExtensions",
                "ViciOne.ServiceBus.DynamoDb.IDynamoDbSagaRepositoryConfigurator",
                "ViciOne.ServiceBus.DynamoDb.IDynamoDbSagaRepositoryConfigurator`1",
            ],
            exportedTypes);

        Assert.Equal(
            ["UseDynamoDb", "UseDynamoDbForRegisteredSagas"],
            typeof(DynamoDbSagaRepositoryRegistrationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
        string[] optionPropertyNames =
        [
            nameof(DynamoDbSagaRepositoryOptions<>.AllowEmptyStrings),
            nameof(DynamoDbSagaRepositoryOptions<>.ConsistentRead),
            nameof(DynamoDbSagaRepositoryOptions<>.EntryConversion),
            nameof(DynamoDbSagaRepositoryOptions<>.RetrieveDateTimeAsUtc),
            nameof(DynamoDbSagaRepositoryOptions<>.TableName),
            nameof(DynamoDbSagaRepositoryOptions<>.TimeProvider),
            nameof(DynamoDbSagaRepositoryOptions<>.TimeToLive),
        ];
        Assert.Equal(
            optionPropertyNames,
            typeof(DynamoDbSagaRepositoryOptions<TestSaga>)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            optionPropertyNames,
            typeof(IDynamoDbSagaRepositoryConfigurator)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.All(
            typeof(DynamoDbSagaRepositoryOptions<TestSaga>).GetProperties(),
            property => Assert.False(property.CanWrite));
        MethodInfo[] contextFactoryMethods =
        [
            .. typeof(IDynamoDbSagaRepositoryConfigurator)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsSpecialName),
        ];
        Assert.Equal(2, contextFactoryMethods.Length);
        Assert.All(contextFactoryMethods, method => Assert.Equal("UseContextFactory", method.Name));
        Assert.All(
            contextFactoryMethods,
            method => Assert.Equal("contextFactory", Assert.Single(method.GetParameters()).Name));
        ConstructorInfo completeOptionsConstructor = Assert.Single(
            typeof(DynamoDbSagaRepositoryOptions<TestSaga>).GetConstructors(),
            constructor => constructor.GetParameters().Length == 7);
        Assert.Equal(
            [
                "tableName",
                "timeToLive",
                "timeProvider",
                "consistentRead",
                "allowEmptyStrings",
                "retrieveDateTimeAsUtc",
                "entryConversion",
            ],
            completeOptionsConstructor.GetParameters().Select(parameter => parameter.Name));
        MethodInfo createMethod = Assert.Single(
            typeof(DynamoDbSagaRepository).GetMethods(BindingFlags.Public | BindingFlags.Static));
        Assert.Equal(
            ["contextFactory", "options"],
            createMethod.GetParameters().Select(parameter => parameter.Name));
        Assert.True(typeof(DynamoDbSagaConcurrencyException).IsSealed);
        Assert.Equal(2, typeof(DynamoDbSagaConcurrencyException).GetConstructors().Length);

        var options = new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table");
        IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        ISagaRegistrationConfigurator<TestSaga> sagaRegistration =
            DispatchProxy.Create<ISagaRegistrationConfigurator<TestSaga>, UnsupportedInvocationProxy>();
        IRegistrationConfigurator registration =
            DispatchProxy.Create<IRegistrationConfigurator, UnsupportedInvocationProxy>();

        Assert.Throws<ArgumentNullException>(
            () => DynamoDbSagaRepository.Create<TestSaga>(null!, options));
        Assert.Throws<ArgumentNullException>(
            () => DynamoDbSagaRepository.Create<TestSaga>(() => context, null!));
        Assert.Throws<ArgumentNullException>(
            () => DynamoDbSagaRepositoryRegistrationExtensions.UseDynamoDb<TestSaga>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => sagaRegistration.UseDynamoDb(null!));
        Assert.Throws<ArgumentNullException>(
            () => DynamoDbSagaRepositoryRegistrationExtensions.UseDynamoDbForRegisteredSagas(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => registration.UseDynamoDbForRegisteredSagas(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "registered-context-factory-snapshot-is-immutable")]
    public void RegisteredContextFactory_IsFrozenAgainstRetainedConfiguratorMutation(bool useServiceProviderFactory)
    {
        IDynamoDBContext registered = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        IDynamoDBContext later = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        IDynamoDbSagaRepositoryConfigurator<TestSaga>? retained = null;

        var services = new ServiceCollection();
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.AddSaga<TestSaga>()
                .UseDynamoDb(repository =>
                {
                    retained = repository;
                    repository.TableName = "valid-table";
                    if (useServiceProviderFactory)
                        repository.UseContextFactory(_ => registered);
                    else
                        repository.UseContextFactory(() => registered);
                });

            Assert.NotNull(retained);
            if (useServiceProviderFactory)
                retained.UseContextFactory(_ => later);
            else
                retained.UseContextFactory(() => later);
        });

        ServiceDescriptor descriptor = Assert.Single(
            services,
            item => item.ServiceType == typeof(DynamoDbSagaContextFactory<TestSaga>));
        Assert.NotNull(descriptor.ImplementationFactory);
        var runtimeFactory = Assert.IsType<DynamoDbSagaContextFactory<TestSaga>>(
            descriptor.ImplementationFactory(EmptyServiceProvider.Instance));

        IDynamoDBContext actual = runtimeFactory.Create();
        Assert.Same(registered, actual);
        Assert.NotSame(later, actual);
        Assert.DoesNotContain(services, item => item.ServiceType == typeof(Func<IDynamoDBContext>));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "multiple-saga-types-own-independent-context-factories")]
    public void MultipleSagaTypes_OwnIndependentContextFactoriesInEitherRegistrationOrder(bool reverseRegistrationOrder)
    {
        IDynamoDBContext firstContext = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        IDynamoDBContext secondContext = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        var services = new ServiceCollection();

        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            void RegisterFirst() => configuration.AddSaga<TestSaga>()
                .UseDynamoDb(repository =>
                {
                    repository.TableName = "first-saga-table";
                    repository.UseContextFactory(() => firstContext);
                });

            void RegisterSecond() => configuration.AddSaga<SecondSaga>()
                .UseDynamoDb(repository =>
                {
                    repository.TableName = "second-saga-table";
                    repository.UseContextFactory(() => secondContext);
                });

            if (reverseRegistrationOrder)
            {
                RegisterSecond();
                RegisterFirst();
            }
            else
            {
                RegisterFirst();
                RegisterSecond();
            }
        });

        DynamoDbSagaContextFactory<TestSaga> firstFactory = ResolveRegisteredFactory<TestSaga>(services);
        DynamoDbSagaContextFactory<SecondSaga> secondFactory = ResolveRegisteredFactory<SecondSaga>(services);

        Assert.Same(firstContext, firstFactory.Create());
        Assert.Same(secondContext, secondFactory.Create());
        Assert.NotSame(firstFactory.Create(), secondFactory.Create());
        Assert.DoesNotContain(services, item => item.ServiceType == typeof(Func<IDynamoDBContext>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "runtime-type-provider-configures-every-versioned-saga")]
    public void RuntimeTypeProvider_ConfiguresEveryRegisteredVersionedSaga()
    {
        IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>();
        int configurationCount = 0;
        var services = new ServiceCollection();

        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.UseDynamoDbForRegisteredSagas(repository =>
            {
                configurationCount++;
                repository.TableName = "shared-saga-table";
                repository.UseContextFactory(() => context);
            });
            configuration.AddSaga<TestSaga>();
            configuration.AddSaga<SecondSaga>();
        });

        Assert.Equal(2, configurationCount);
        Assert.Same(context, ResolveRegisteredFactory<TestSaga>(services).Create());
        Assert.Same(context, ResolveRegisteredFactory<SecondSaga>(services).Create());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "invalid-input-rejected-and-runtime-options-frozen")]
    public void InvalidAndMutableInput_IsRejectedOrFrozenBeforeRegistration()
    {
        string?[] invalidTableNames =
        [
            null,
            string.Empty,
            "ab",
            new('a', 256),
            "contains space",
            "contains/slash",
        ];

        Assert.All(
            invalidTableNames,
            tableName => Assert.Throws<ArgumentException>(() => new DynamoDbSagaRepositoryOptions<TestSaga>(tableName!)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table", TimeSpan.FromSeconds(30) - TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentNullException>(
            () => new DynamoDbSagaRepositoryOptions<TestSaga>("valid-table", TimeSpan.FromSeconds(30), null!));
        var unsupportedConversion = (DynamoDBEntryConversion)RuntimeHelpers.GetUninitializedObject(
            typeof(DynamoDBEntryConversion));
        Assert.Throws<ArgumentException>(
            () => new DynamoDbSagaRepositoryOptions<TestSaga>(
                "valid-table",
                TimeSpan.FromSeconds(30),
                TimeProvider.System,
                entryConversion: unsupportedConversion));
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>(
            "valid.table_name-01",
            TimeSpan.FromSeconds(30),
            TimeProvider.System,
            consistentRead: false,
            allowEmptyStrings: false,
            retrieveDateTimeAsUtc: false,
            entryConversion: DynamoDBEntryConversion.V1);

        LoadConfig firstLoad = options.CreateLoadConfig();
        firstLoad.OverrideTableName = "mutated-table";
        firstLoad.ConsistentRead = true;
        GetTargetTableConfig firstTarget = options.CreateTargetTableConfig();
        firstTarget.OverrideTableName = "mutated-target";

        LoadConfig secondLoad = options.CreateLoadConfig();
        GetTargetTableConfig secondTarget = options.CreateTargetTableConfig();
        DeleteConfig delete = options.CreateDeleteConfig();

        Assert.NotSame(firstLoad, secondLoad);
        Assert.NotSame(firstTarget, secondTarget);
        Assert.Equal("valid.table_name-01", secondLoad.OverrideTableName);
        Assert.Equal("valid.table_name-01", secondTarget.OverrideTableName);
        Assert.Equal("valid.table_name-01", delete.OverrideTableName);
        Assert.False(secondLoad.ConsistentRead);
        Assert.False(secondLoad.IsEmptyStringValueEnabled);
        Assert.False(secondLoad.RetrieveDateTimeInUtc);
        Assert.Same(DynamoDBEntryConversion.V1, secondLoad.Conversion);
        Assert.Same(DynamoDBEntryConversion.V1, secondTarget.Conversion);
        Assert.Same(DynamoDBEntryConversion.V1, delete.Conversion);
        Assert.Equal(TimeSpan.FromSeconds(30), options.TimeToLive);
        Assert.Same(TimeProvider.System, options.TimeProvider);
        Assert.False(options.ConsistentRead);
        Assert.False(options.AllowEmptyStrings);
        Assert.False(options.RetrieveDateTimeAsUtc);
        Assert.Same(DynamoDBEntryConversion.V1, options.EntryConversion);

        Assert.DoesNotContain(
            typeof(IDynamoDbSagaRepositoryConfigurator).GetProperties(),
            property => property.Name is "LockSuffix" or "LockTimeout");
        Assert.DoesNotContain(
            typeof(DynamoDbSagaRepositoryOptions<TestSaga>).GetProperties(),
            property => property.CanWrite || property.PropertyType == typeof(DynamoDBOperationConfig));

        var configurator = new DynamoDbSagaRepositoryConfigurator<TestSaga>
        {
            TableName = "valid-table",
            TimeProvider = null!,
        };
        configurator.UseContextFactory(() => null!);
        ValidationResult validation = Assert.Single(configurator.Validate());
        Assert.Equal("TimeProvider", validation.Key);
        Assert.Throws<InvalidOperationException>(
            () => new DynamoDbSagaContextFactory<TestSaga>(() => null!).Create());

        Assert.Throws<ArgumentNullException>(() => configurator.UseContextFactory((Func<IDynamoDBContext>)null!));
        Assert.Throws<ArgumentNullException>(
            () => configurator.UseContextFactory((Func<IServiceProvider, IDynamoDBContext>)null!));
        Assert.Throws<ArgumentNullException>(() => configurator.Register(null!));
        Assert.Throws<ArgumentNullException>(() => new DynamoDbSagaRepositoryRegistrationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new DynamoDbSagaStore<TestSaga>(null!, options));
        Assert.Throws<ArgumentNullException>(
            () => new DynamoDbSagaStore<TestSaga>(
                DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>(),
                null!));
    }

    private sealed class TestSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }

    private sealed class SecondSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }

    private static DynamoDbSagaContextFactory<TSaga> ResolveRegisteredFactory<TSaga>(IServiceCollection services)
        where TSaga : class, ISagaVersion
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            item => item.ServiceType == typeof(DynamoDbSagaContextFactory<TSaga>));
        Assert.NotNull(descriptor.ImplementationFactory);
        return Assert.IsType<DynamoDbSagaContextFactory<TSaga>>(
            descriptor.ImplementationFactory(EmptyServiceProvider.Instance));
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
