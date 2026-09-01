using ViciOne.ServiceBus.Tests.Infrastructure.Databases;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Infrastructure;

[Collection("ProcessEnvironment")]
public sealed class RunnerContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RUN-SCOPED-DATABASE-IDENTITY", "runner-root-is-part-of-name")]
    public void DatabaseName_IsStableWithinOneRunAndDifferentBetweenRuns()
    {
        const string runRootVariable = "VICIONE_SERVICEBUS_RUN_ROOT";
        string first;
        string repeated;
        using (EnvironmentSnapshot.WithValues(new Dictionary<string, string?>
               {
                   [runRootVariable] = "/private/tmp/vicione-run-a",
               }))
        {
            first = TestDatabaseName.CreateForCurrentRun("vsbpg", "sql-run-scope", "delivery");
            repeated = TestDatabaseName.CreateForCurrentRun("vsbpg", "sql-run-scope", "delivery");
        }

        string second;
        using (EnvironmentSnapshot.WithValues(new Dictionary<string, string?>
               {
                   [runRootVariable] = "/private/tmp/vicione-run-b",
               }))
        {
            second = TestDatabaseName.CreateForCurrentRun("vsbpg", "sql-run-scope", "delivery");
        }

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, second);
        Assert.StartsWith("vsbpg_", first, StringComparison.Ordinal);
        Assert.Equal(38, first.Length);
        Assert.Equal(38, second.Length);
    }

    private sealed class EnvironmentSnapshot : IDisposable
    {
        private readonly IReadOnlyDictionary<string, string?> _original;

        private EnvironmentSnapshot(IReadOnlyDictionary<string, string?> values)
        {
            _original = values.Keys.ToDictionary(name => name, Environment.GetEnvironmentVariable);
            foreach ((string name, string? value) in values)
                Environment.SetEnvironmentVariable(name, value);
        }

        public static EnvironmentSnapshot WithValues(IReadOnlyDictionary<string, string?> values) => new(values);

        public void Dispose()
        {
            foreach ((string name, string? value) in _original)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}

[CollectionDefinition("ProcessEnvironment", DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection;
