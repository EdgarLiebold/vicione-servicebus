using NDesk.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark.BusOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

[Collection(ProcessStateCollection.Name)]
public sealed class BusOutboxDatabaseSettingsTests
{
    private const string VariableName = "VICIONE_BENCHMARK_TEST_SQLSERVER_CONNECTION_STRING";

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-DATABASE", "missing-sqlserver-configuration-fails")]
    public void ResolveConnectionString_WhenConfigurationIsMissing_FailsBeforeConnecting()
    {
        WithEnvironmentVariable(null, () =>
            Assert.Throws<OptionException>(() => BusOutboxDatabaseSettings.ResolveConnectionString(VariableName)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-DATABASE", "localdb-is-rejected")]
    public void ResolveConnectionString_WhenLocalDbIsConfigured_RejectsWindowsOnlyEndpoint()
    {
        const string connectionString =
            @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=ViciOneServiceBusBenchmark;Integrated Security=True";

        WithEnvironmentVariable(connectionString, () =>
        {
            OptionException exception = Assert.Throws<OptionException>(
                () => BusOutboxDatabaseSettings.ResolveConnectionString(VariableName));
            Assert.Contains("LocalDB", exception.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-DATABASE", "explicit-sqlserver-settings-returned")]
    public void ResolveConnectionString_WhenServerAndDatabaseAreExplicit_ReturnsValidatedConfiguration()
    {
        const string connectionString =
            "Server=tcp:db.example.invalid,1433;Initial Catalog=ViciOneServiceBusBenchmark;Integrated Security=True;Encrypt=True";

        WithEnvironmentVariable(connectionString, () =>
        {
            string result = BusOutboxDatabaseSettings.ResolveConnectionString(VariableName);

            Assert.Contains("db.example.invalid", result, StringComparison.Ordinal);
            Assert.Contains("ViciOneServiceBusBenchmark", result, StringComparison.Ordinal);
        });
    }

    private static void WithEnvironmentVariable(string? value, Action assertion)
    {
        string? previous = Environment.GetEnvironmentVariable(VariableName);
        try
        {
            Environment.SetEnvironmentVariable(VariableName, value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, previous);
        }
    }
}
