namespace ViciOne.ServiceBus.DynamoDbIntegration.Tests.DynamoDbIntegration.Saga;

using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDbIntegration.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class DynamoDbSagaRepositoryConfigurationTests
{
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
                .DynamoDbRepository(repository =>
                {
                    retained = repository;
                    repository.TableName = "valid-table";
                    if (useServiceProviderFactory)
                        repository.ContextFactory(_ => registered);
                    else
                        repository.ContextFactory(() => registered);
                });

            Assert.NotNull(retained);
            if (useServiceProviderFactory)
                retained.ContextFactory(_ => later);
            else
                retained.ContextFactory(() => later);
        });

        ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(Func<IDynamoDBContext>));
        Assert.NotNull(descriptor.ImplementationFactory);
        var runtimeFactory = Assert.IsType<Func<IDynamoDBContext>>(descriptor.ImplementationFactory(EmptyServiceProvider.Instance));

        IDynamoDBContext actual = runtimeFactory();
        Assert.Same(registered, actual);
        Assert.NotSame(later, actual);
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
        var options = new DynamoDbSagaRepositoryOptions<TestSaga>(
            "valid.table_name-01",
            TimeSpan.FromSeconds(30),
            TimeProvider.System,
            consistentRead: false,
            isEmptyStringValueEnabled: false,
            retrieveDateTimeInUtc: false,
            conversion: DynamoDBEntryConversion.V1);

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
        Assert.Equal(TimeSpan.FromSeconds(30), options.Expiration);
        Assert.Same(TimeProvider.System, options.TimeProvider);

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
        configurator.ContextFactory(() => null!);
        ValidationResult validation = Assert.Single(configurator.Validate());
        Assert.Equal("TimeProvider", validation.Key);

        Assert.Throws<ArgumentNullException>(() => configurator.ContextFactory((Func<IDynamoDBContext>)null!));
        Assert.Throws<ArgumentNullException>(() => configurator.ContextFactory((Func<IServiceProvider, IDynamoDBContext>)null!));
        Assert.Throws<ArgumentNullException>(() => configurator.Register(null!));
        Assert.Throws<ArgumentNullException>(() => new DynamoDbSagaRepositoryRegistrationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new DynamoDbDatabaseContext<TestSaga>(null!, options));
        Assert.Throws<ArgumentNullException>(
            () => new DynamoDbDatabaseContext<TestSaga>(DispatchProxy.Create<IDynamoDBContext, UnsupportedInvocationProxy>(), null!));
    }

    private sealed class TestSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
