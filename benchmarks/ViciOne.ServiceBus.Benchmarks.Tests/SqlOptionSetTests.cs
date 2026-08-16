namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System;
using NDesk.Options;
using NUnit.Framework;
using ViciOneServiceBusBenchmark;


[NonParallelizable]
public class SqlOptionSetTests
{
    const string VariableName = "VICIONE_BENCHMARK_TEST_POSTGRES_ADMIN_PASSWORD";

    [Test]
    public void ResolveAdminPassword_WhenConfigurationIsMissing_FailsBeforeConnecting()
    {
        WithEnvironmentVariable(null, () =>
        {
            var options = CreateOptions();

            Assert.That(() => options.ResolveAdminPassword(), Throws.TypeOf<OptionException>());
        });
    }

    [Test]
    public void ResolveAdminPassword_WhenConfigured_ReturnsEnvironmentValue()
    {
        WithEnvironmentVariable("test-only-secret", () =>
        {
            var options = CreateOptions();

            Assert.That(options.ResolveAdminPassword(), Is.EqualTo("test-only-secret"));
        });
    }

    static SqlOptionSet CreateOptions()
    {
        var options = new SqlOptionSet();
        options.Parse(new[] { $"--admin-password-env={VariableName}" });
        return options;
    }

    static void WithEnvironmentVariable(string value, Action assertion)
    {
        var previous = Environment.GetEnvironmentVariable(VariableName);
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
