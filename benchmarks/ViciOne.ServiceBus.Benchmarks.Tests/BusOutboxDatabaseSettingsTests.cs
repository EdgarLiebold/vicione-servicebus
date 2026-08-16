namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System;
using NDesk.Options;
using NUnit.Framework;
using ViciOneServiceBusBenchmark.BusOutbox;


[NonParallelizable]
public class BusOutboxDatabaseSettingsTests
{
    const string VariableName = "VICIONE_BENCHMARK_TEST_SQLSERVER_CONNECTION_STRING";

    [Test]
    public void ResolveConnectionString_WhenConfigurationIsMissing_FailsBeforeConnecting()
    {
        WithEnvironmentVariable(null, () =>
            Assert.That(() => BusOutboxDatabaseSettings.ResolveConnectionString(VariableName),
                Throws.TypeOf<OptionException>()));
    }

    [Test]
    public void ResolveConnectionString_WhenLocalDbIsConfigured_RejectsWindowsOnlyEndpoint()
    {
        const string connectionString =
            @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=ViciOneServiceBusBenchmark;Integrated Security=True";

        WithEnvironmentVariable(connectionString, () =>
            Assert.That(() => BusOutboxDatabaseSettings.ResolveConnectionString(VariableName),
                Throws.TypeOf<OptionException>().With.Message.Contains("LocalDB")));
    }

    [Test]
    public void ResolveConnectionString_WhenServerAndDatabaseAreExplicit_ReturnsValidatedConfiguration()
    {
        const string connectionString =
            "Server=tcp:db.example.invalid,1433;Initial Catalog=ViciOneServiceBusBenchmark;Integrated Security=True;Encrypt=True";

        WithEnvironmentVariable(connectionString, () =>
        {
            var result = BusOutboxDatabaseSettings.ResolveConnectionString(VariableName);

            Assert.That(result, Does.Contain("db.example.invalid"));
            Assert.That(result, Does.Contain("ViciOneServiceBusBenchmark"));
        });
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
