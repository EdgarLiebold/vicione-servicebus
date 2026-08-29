using System.Reflection;
using ViciOne.ServiceBus.TestInfrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Infrastructure;

[Collection("ProcessEnvironment")]
public sealed class RunnerContractTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0048", "native-owner")]
    public void DatabaseEndpoint_ExposesCompleteEndpoint()
    {
        var endpoint = new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 32768, "test_user", "test-secret");

        Assert.Equal("PostgreSQL", endpoint.Engine);
        Assert.Equal("127.0.0.1", endpoint.Host);
        Assert.Equal(32768, endpoint.Port);
        Assert.Equal("test_user", endpoint.Username);
        Assert.Equal("test-secret", endpoint.Password);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0049", "native-owner")]
    public void DatabaseEndpoint_RejectsEveryMissingRequiredPart()
    {
        Assert.Throws<ArgumentNullException>(() => new DatabaseEndpoint(null!, "127.0.0.1", 1, "u", "p"));
        Assert.Throws<ArgumentNullException>(() => new DatabaseEndpoint("PostgreSQL", null!, 1, "u", "p"));
        Assert.Throws<ArgumentNullException>(() => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 1, null!, "p"));
        Assert.Throws<ArgumentNullException>(() => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 1, "u", null!));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0050", "native-owner")]
    public void DatabaseEndpoint_RejectsPortsOutsideTcpRange()
    {
        int[] invalidPorts = [0, -1, 65536];

        Assert.All(invalidPorts, port => Assert.Throws<ArgumentOutOfRangeException>(
            () => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", port, "u", "p")));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0051", "native-owner")]
    public void DatabaseEndpoint_DescriptionNeverDisclosesPassword()
    {
        const string password = "recognizable-test-secret";
        var endpoint = new DatabaseEndpoint("SQL Server", "127.0.0.1", 32769, "test_user", password);

        string description = endpoint.ToString();

        Assert.DoesNotContain(password, description, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1", description, StringComparison.Ordinal);
        Assert.Contains("32769", description, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0052", "native-owner")]
    public void TestRunnerContract_NamesEveryMissingVariableBeforeConnectionAttempt()
    {
        string[] variables = PostgresVariables();
        using var environment = EnvironmentSnapshot.Cleared(variables);

        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => ReadPostgres(variables));
        var exception = Assert.IsType<TestRunnerContractException>(wrapper.InnerException);

        Assert.All(variables, variable => Assert.Contains(variable, exception.Message, StringComparison.Ordinal));
        Assert.Contains("no connection is attempted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0053", "native-owner")]
    public void TestRunnerContract_RejectsNonNumericPublishedPort()
    {
        string[] variables = PostgresVariables();
        using var environment = EnvironmentSnapshot.WithValues(new Dictionary<string, string?>
        {
            [variables[0]] = "127.0.0.1",
            [variables[1]] = "not-a-port",
            [variables[2]] = "test_user",
            [variables[3]] = "test_password",
        });

        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => ReadPostgres(variables));
        var exception = Assert.IsType<TestRunnerContractException>(wrapper.InnerException);

        Assert.Contains(TestRunnerContract.PostgresPortVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0054", "native-owner")]
    public void LocalConfiguration_RejectsAnUnknownResourceSelection()
    {
        var options = new ViciOneTestOptions
        {
            Profile = TestProfile.LocalIntegration,
            OperationTimeout = TimeSpan.FromSeconds(1),
            LocalInfrastructure = new LocalInfrastructureOptions(),
        };

        IReadOnlyList<string> findings = options.ValidateForLocal(
            (LocalTestResource)int.MaxValue);

        Assert.Contains("LocalInfrastructure:Selection", findings);
    }

    private static string[] PostgresVariables() =>
    [
        TestRunnerContract.PostgresHostVariable,
        TestRunnerContract.PostgresPortVariable,
        TestRunnerContract.PostgresUsernameVariable,
        TestRunnerContract.PostgresPasswordVariable,
    ];

    private static void ReadPostgres(string[] variables)
    {
        MethodInfo read = typeof(TestRunnerContract).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TestRunnerContract.Read is missing.");
        read.Invoke(null, ["PostgreSQL", variables[0], variables[1], variables[2], variables[3]]);
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

        public static EnvironmentSnapshot Cleared(IEnumerable<string> variables) =>
            WithValues(variables.ToDictionary(name => name, _ => (string?)null));

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
