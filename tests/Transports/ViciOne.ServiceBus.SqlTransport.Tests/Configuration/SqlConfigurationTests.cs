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
        var settings = new SqlServerHostSettings(new SqlTransportOptions
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
        host.Settings = new SqlServerHostSettings(new SqlTransportOptions
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

        Assert.Equal(
            [
                (ValidationResultDisposition.Failure, "invalid queue/name", (string?)null, "Must be a valid queue name"),
                (ValidationResultDisposition.Warning, "invalid queue/name", (string?)null, "Existing messages will be purged on service start"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "MaintenanceBatchSize", "Must be >= 1"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "AutoDeleteOnIdle", "Must be greater than zero when specified"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "PollingInterval", "Must be greater than zero"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "LockDuration", "Must be >= 1 second"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "MaxLockDuration", "Must be greater than or equal to LockDuration"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "MaxDeliveryCount", "Must be greater than zero when specified"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "UnlockDelay", "Must not be less than TimeSpan.Zero"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "ConcurrentDeliveryLimit", "Must be greater than zero"),
                (ValidationResultDisposition.Failure, "invalid queue/name", "ReceiveMode", "Must be a defined SQL receive mode"),
            ],
            results.Select(result => (result.Disposition, result.Key, result.Value, result.Message)));

        var oversizedEndpoint = Assert.IsType<SqlReceiveEndpointConfiguration>(
            host.CreateReceiveEndpointConfiguration("oversized_queue", configurator =>
                configurator.AutoDeleteOnIdle = TimeSpan.FromSeconds(int.MaxValue) + TimeSpan.FromTicks(1)));

        Assert.Contains(oversizedEndpoint.Validate(), result =>
            result.Disposition == ValidationResultDisposition.Failure
            && result.Value == "AutoDeleteOnIdle"
            && result.Message == "Must not exceed the SQL seconds limit");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVE-VALIDATION", "valid-boundaries-are-accepted")]
    public void ReceiveEndpointValidation_AcceptsEveryValidBoundary()
    {
        var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
        var bus = new SqlBusConfiguration(topology);
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerHostSettings(new SqlTransportOptions
        {
            Host = "localhost",
            Database = "transport_tests",
            Schema = "transport",
            Username = "test_user",
            Password = "test_password",
        });
        var endpoint = Assert.IsType<SqlReceiveEndpointConfiguration>(
            host.CreateReceiveEndpointConfiguration("valid-queue_1:segment.name", configurator =>
            {
                configurator.AutoDeleteOnIdle = TimeSpan.FromTicks(1);
                configurator.MaintenanceBatchSize = 1;
                configurator.PollingInterval = TimeSpan.FromTicks(1);
                configurator.LockDuration = TimeSpan.FromSeconds(1);
                configurator.MaxLockDuration = TimeSpan.FromSeconds(1);
                configurator.MaxDeliveryCount = 1;
                configurator.UnlockDelay = TimeSpan.Zero;
                configurator.SetReceiveMode(SqlReceiveMode.Partitioned, 1);
            }));

        Assert.Empty(endpoint.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-PARTITIONED-RECEIVE", "transport-neutral-capability-selects-sql-partitioning")]
    public void PartitionedReceiveCapability_SelectsSqlPartitionedMode()
    {
        var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
        var bus = new SqlBusConfiguration(topology);
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerHostSettings(new SqlTransportOptions
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
