using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Configuration;

public sealed class SqlConfigurationTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0021", "native-owner")]
    public void SqlServerOptions_BracketIpv6HostInBusAddress()
    {
        var settings = new SqlServerSqlHostSettings(new SqlTransportOptions
        {
            Host = "::1",
            Database = "transport_tests",
            Schema = "transport",
            Username = "test_user",
            Password = "test_password",
        });

        Assert.Equal("[::1]", settings.HostAddress.Host);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0126", "native-owner")]
    public void ReceiveEndpointValidation_CoversEntityMaintenanceUnlockAndPurgeBoundaries()
    {
        var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
        var bus = new SqlBusConfiguration(topology);
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerSqlHostSettings(new SqlTransportOptions
        {
            Host = "localhost",
            Database = "transport_tests",
            Schema = "transport",
            Username = "test_user",
            Password = "test_password",
        });
        var endpoint = Assert.IsType<SqlReceiveEndpointConfiguration>(
            host.CreateReceiveEndpointConfiguration("invalid queue/name", configurator =>
            {
                configurator.AutoDeleteOnIdle = TimeSpan.Zero;
                configurator.PurgeOnStartup = true;
                configurator.MaintenanceBatchSize = 0;
                configurator.PollingInterval = TimeSpan.Zero;
                configurator.LockDuration = TimeSpan.FromMilliseconds(500);
                configurator.MaxLockDuration = TimeSpan.FromMilliseconds(250);
                configurator.MaxDeliveryCount = 0;
                configurator.UnlockDelay = TimeSpan.FromTicks(-1);
                configurator.SetReceiveMode((SqlReceiveMode)99, 0);
            }));

        ValidationResult[] results = endpoint.Validate().ToArray();

        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && result.Message.Contains("valid queue name", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Warning
            && result.Message.Contains("purged", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "MaintenanceBatchSize", StringComparison.Ordinal)
            && result.Message.Contains(">= 1", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "UnlockDelay", StringComparison.Ordinal)
            && result.Message.Contains("TimeSpan.Zero", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "AutoDeleteOnIdle", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "PollingInterval", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "MaxLockDuration", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "MaxDeliveryCount", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "ConcurrentDeliveryLimit", StringComparison.Ordinal));
        Assert.Contains(results, result => result.Disposition == ValidationResultDisposition.Failure
            && string.Equals(result.Value, "ReceiveMode", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PARTITIONED-RECEIVE", "transport-neutral-capability-selects-sql-partitioning")]
    public void PartitionedReceiveCapability_SelectsSqlPartitionedMode()
    {
        var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
        var bus = new SqlBusConfiguration(topology);
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerSqlHostSettings(new SqlTransportOptions
        {
            Host = "localhost",
            Database = "transport_tests",
            Schema = "transport",
            Username = "test_user",
            Password = "test_password",
        });
        var endpoint = Assert.IsType<SqlReceiveEndpointConfiguration>(
            host.CreateReceiveEndpointConfiguration("partitioned_jobs", null));

        var partitionedConfigurator = Assert.IsAssignableFrom<IPartitionedReceiveEndpointConfigurator>(endpoint);
        partitionedConfigurator.SetPartitionedReceive();

        Assert.Equal(SqlReceiveMode.Partitioned, endpoint.Settings.ReceiveMode);
        Assert.Equal(1, endpoint.Settings.ConcurrentDeliveryLimit);
    }
}
