using System.Reflection;
using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDb.Configuration;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.Tests.Saga;

public sealed class DynamoDbRegistrationValidationTests
{
    [Theory]
    [InlineData("factory", "ContextFactory", "must be specified")]
    [InlineData("null-table", "TableName", "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period")]
    [InlineData("short-table", "TableName", "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period")]
    [InlineData("long-table", "TableName", "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period")]
    [InlineData("invalid-table", "TableName", "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period")]
    [InlineData("negative-ttl", "TimeToLive", "If specified, must be at least 30 seconds")]
    [InlineData("zero-ttl", "TimeToLive", "If specified, must be at least 30 seconds")]
    [InlineData("short-ttl", "TimeToLive", "If specified, must be at least 30 seconds")]
    [InlineData("clock", "TimeProvider", "must be specified")]
    [InlineData("null-conversion", "EntryConversion", "must be the immutable DynamoDBEntryConversion.V1 or V2 instance")]
    [InlineData("unsupported-conversion", "EntryConversion", "must be the immutable DynamoDBEntryConversion.V1 or V2 instance")]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "invalid-registration-reports-diagnostic-before-side-effects")]
    public void InvalidConfiguration_IsRejectedBeforeRepositoryRegistration(string fault, string key, string message)
    {
        var registration = DispatchProxy.Create<ISagaRegistrationConfigurator<TestSaga>, RegistrationProbe>();
        var probe = (RegistrationProbe)(object)registration;
        int configureCalls = 0;
        int contextCalls = 0;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => registration.UseDynamoDb(configuration =>
        {
            configureCalls++;
            configuration.TableName = "valid-table";
            if (fault != "factory")
                configuration.UseContextFactory(() =>
                {
                    contextCalls++;
                    throw new InvalidOperationException("Validation must not create a context.");
                });

            switch (fault)
            {
                case "null-table": configuration.TableName = null!; break;
                case "short-table": configuration.TableName = "ab"; break;
                case "long-table": configuration.TableName = new string('a', 256); break;
                case "invalid-table": configuration.TableName = "invalid/table"; break;
                case "negative-ttl": configuration.TimeToLive = TimeSpan.FromTicks(-1); break;
                case "zero-ttl": configuration.TimeToLive = TimeSpan.Zero; break;
                case "short-ttl": configuration.TimeToLive = TimeSpan.FromSeconds(30) - TimeSpan.FromTicks(1); break;
                case "clock": configuration.TimeProvider = null!; break;
                case "null-conversion": configuration.EntryConversion = null!; break;
                case "unsupported-conversion":
                    configuration.EntryConversion = (DynamoDBEntryConversion)RuntimeHelpers.GetUninitializedObject(typeof(DynamoDBEntryConversion));
                    break;
                case "factory": break;
                default: throw new ArgumentOutOfRangeException(nameof(fault));
            }
        }));

        ValidationResult result = Assert.Single(exception.Results);
        Assert.Equal(key, result.Key);
        Assert.Equal(message, result.Message);
        Assert.Equal(ValidationResultDisposition.Failure, result.Disposition);
        Assert.Contains("The Amazon DynamoDB saga repository configuration is invalid:", exception.Message);
        Assert.Contains(key, exception.Message);
        Assert.Contains(message, exception.Message);
        Assert.Equal(1, configureCalls);
        Assert.Equal(0, probe.Calls);
        Assert.Equal(0, contextCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "all-validation-failures-are-reported-and-correctable")]
    public void MultipleFailures_AreReportedTogetherAndCorrectedConfigurationIsRevalidated()
    {
        var registration = DispatchProxy.Create<ISagaRegistrationConfigurator<TestSaga>, RegistrationProbe>();
        var probe = (RegistrationProbe)(object)registration;
        DynamoDbSagaRepositoryConfigurator<TestSaga>? retained = null;
        int configureCalls = 0;
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => registration.UseDynamoDb(configuration =>
        {
            configureCalls++;
            retained = Assert.IsType<DynamoDbSagaRepositoryConfigurator<TestSaga>>(configuration);
            configuration.TableName = "ab";
            configuration.TimeToLive = TimeSpan.Zero;
            configuration.TimeProvider = null!;
            configuration.EntryConversion = null!;
        }));

        (string Key, string Message)[] expected =
        [
            ("ContextFactory", "must be specified"),
            ("TableName", "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period"),
            ("TimeToLive", "If specified, must be at least 30 seconds"),
            ("TimeProvider", "must be specified"),
            ("EntryConversion", "must be the immutable DynamoDBEntryConversion.V1 or V2 instance"),
        ];
        Assert.Equal(expected, exception.Results.Select(result => (result.Key, result.Message)));
        Assert.All(exception.Results, result => Assert.Equal(ValidationResultDisposition.Failure, result.Disposition));
        foreach ((string key, string message) in expected)
        {
            Assert.Contains(key, exception.Message);
            Assert.Contains(message, exception.Message);
        }
        Assert.NotNull(retained);
        Assert.Equal(expected, retained.Validate().Select(result => (result.Key, result.Message)));
        Assert.Equal(1, configureCalls);
        Assert.Equal(0, probe.Calls);

        int contextCalls = 0;
        retained.UseContextFactory(() =>
        {
            contextCalls++;
            throw new InvalidOperationException("Validation must not create a context.");
        });
        retained.TableName = "valid-table";
        retained.TimeToLive = TimeSpan.FromSeconds(30);
        retained.TimeProvider = TimeProvider.System;
        retained.EntryConversion = DynamoDBEntryConversion.V1;
        Assert.Empty(retained.Validate());
        Assert.Equal(0, contextCalls);
        Assert.Equal(expected, exception.Results.Select(result => (result.Key, result.Message)));
    }

    [Theory]
    [InlineData(false, false, 3)]
    [InlineData(true, true, 255)]
    [InlineData(false, true, 255)]
    [InlineData(true, false, 3)]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-CONFIGURATION", "valid-boundaries-register-frozen-options-and-lazy-context")]
    public void ValidBoundaries_RegisterFrozenOptionsAndCreateContextLazily(bool providerAware, bool bounded, int tableLength)
    {
        var services = new ServiceCollection();
        string tableName = "A_9" + new string('-', tableLength - 3);
        TimeSpan? ttl = bounded ? TimeSpan.FromSeconds(30) : null;
        var clock = new RepositoryClock();
        DynamoDBEntryConversion conversion = bounded ? DynamoDBEntryConversion.V1 : DynamoDBEntryConversion.V2;
        IDynamoDBContext context = DispatchProxy.Create<IDynamoDBContext, RegistrationProbe>();
        IServiceProvider? actualProvider = null;
        int contextCalls = 0;
        int replacementCalls = 0;
        IDynamoDbSagaRepositoryConfigurator<TestSaga>? retained = null;

        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            ISagaRegistrationConfigurator<TestSaga> saga = configuration.AddSaga<TestSaga>();
            Assert.Same(saga, saga.UseDynamoDb(repository =>
            {
                retained = repository;
                repository.TableName = tableName;
                repository.TimeToLive = ttl;
                repository.TimeProvider = clock;
                repository.ConsistentRead = false;
                repository.AllowEmptyStrings = false;
                repository.RetrieveDateTimeAsUtc = false;
                repository.EntryConversion = conversion;
                if (providerAware)
                    repository.UseContextFactory(provider =>
                    {
                        actualProvider = provider;
                        contextCalls++;
                        return context;
                    });
                else
                    repository.UseContextFactory(() =>
                    {
                        contextCalls++;
                        return context;
                    });
            }));
        });

        Assert.Equal(0, contextCalls);
        Assert.NotNull(retained);
        retained.TableName = "replacement-table";
        retained.TimeToLive = TimeSpan.FromDays(1);
        retained.TimeProvider = TimeProvider.System;
        retained.ConsistentRead = true;
        retained.AllowEmptyStrings = true;
        retained.RetrieveDateTimeAsUtc = true;
        retained.EntryConversion = bounded ? DynamoDBEntryConversion.V2 : DynamoDBEntryConversion.V1;
        retained.UseContextFactory(() =>
        {
            replacementCalls++;
            throw new InvalidOperationException("Registration must retain the original factory.");
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DynamoDbSagaRepositoryOptions<TestSaga>>();
        var factory = provider.GetRequiredService<DynamoDbSagaContextFactory<TestSaga>>();
        Assert.Equal(tableName, options.TableName);
        Assert.Equal(ttl, options.TimeToLive);
        Assert.Same(clock, options.TimeProvider);
        Assert.False(options.ConsistentRead);
        Assert.False(options.AllowEmptyStrings);
        Assert.False(options.RetrieveDateTimeAsUtc);
        Assert.Same(conversion, options.EntryConversion);
        Assert.Equal(0, contextCalls);
        Assert.Same(context, factory.Create());
        Assert.Equal(1, contextCalls);
        Assert.Equal(0, replacementCalls);
        if (providerAware)
            Assert.Same(provider.GetRequiredService<IServiceProvider>(), actualProvider);
        else
            Assert.Null(actualProvider);
    }

    private sealed class RepositoryClock : TimeProvider;

    private sealed class TestSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }

    private class RegistrationProbe : DispatchProxy
    {
        public int Calls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            throw new InvalidOperationException($"Invalid settings reached registration: {targetMethod?.Name}");
        }
    }
}
