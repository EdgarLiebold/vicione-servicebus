using NDesk.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

[Collection(ProcessStateCollection.Name)]
public sealed class SqlOptionSetTests
{
    private const string VariableName = "VICIONE_BENCHMARK_TEST_POSTGRES_ADMIN_PASSWORD";

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-SQL", "missing-admin-password-fails")]
    public void ResolveAdminPassword_WhenConfigurationIsMissing_FailsBeforeConnecting()
    {
        WithEnvironmentVariable(null, () =>
        {
            var options = CreateOptions();

            Assert.Throws<OptionException>(options.ResolveAdminPassword);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-SQL", "configured-admin-password-returned")]
    public void ResolveAdminPassword_WhenConfigured_ReturnsEnvironmentValue()
    {
        WithEnvironmentVariable("test-only-secret", () =>
        {
            var options = CreateOptions();

            Assert.Equal("test-only-secret", options.ResolveAdminPassword());
        });
    }

    private static SqlOptionSet CreateOptions()
    {
        var options = new SqlOptionSet();
        options.Parse([$"--admin-password-env={VariableName}"]);
        return options;
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
