using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Configuration;

public sealed class SqlHostSettingsContractTests
{
    [Theory]
    [InlineData("db://user:p+a%3Ass@localhost:5544/tenant.area", "p+a:ss")]
    [InlineData("db://user:first:second@localhost:5544/tenant.area", "first:second")]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "address-credentials-preserve-password-suffix")]
    public void AddressConstructor_DecodesCredentialsWithoutLosingPasswordSuffix(string address, string password)
    {
        var settings = new TestHostSettings(new Uri(address));

        Assert.Equal("localhost", settings.Host);
        Assert.Equal(5544, settings.Port);
        Assert.Equal("user", settings.Username);
        Assert.Equal(password, settings.Password);
        Assert.Equal("tenant", settings.VirtualHost);
        Assert.Equal("area", settings.Area);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "operational-defaults")]
    public void Defaults_ExposeTheOperationalContract()
    {
        var settings = new TestHostSettings();

        Assert.Equal("/", settings.VirtualHost);
        Assert.Equal(System.Data.IsolationLevel.RepeatableRead, settings.IsolationLevel);
        Assert.Equal(10, settings.ConnectionLimit);
        Assert.True(settings.MaintenanceEnabled);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.MaintenanceInterval);
        Assert.Equal(TimeSpan.FromMinutes(1), settings.QueueCleanupInterval);
        Assert.Equal(10_000, settings.MaintenanceBatchSize);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "invalid-operational-boundaries")]
    public void Validate_ReportsEveryInvalidOperationalBoundary()
    {
        var settings = new TestHostSettings
        {
            Host = " ",
            ConnectionLimit = 0,
            MaintenanceInterval = TimeSpan.Zero,
            QueueCleanupInterval = TimeSpan.Zero,
            MaintenanceBatchSize = 0,
        };

        ValidationResult[] failures = settings.Validate().ToArray();

        Assert.Equal(
            ["Host", "ConnectionLimit", "MaintenanceInterval", "QueueCleanupInterval", "MaintenanceBatchSize"],
            failures.Select(result => result.Key));
        Assert.All(failures, result => Assert.Equal(ValidationResultDisposition.Failure, result.Disposition));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65_536)]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "port-outside-tcp-range")]
    public void Validate_RejectsPortsOutsideTheTcpRange(int port)
    {
        var settings = new TestHostSettings
        {
            Host = "localhost",
            Port = port,
        };

        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal("Port", failure.Key);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "valid-operational-boundaries")]
    public void Validate_AcceptsEveryValidOperationalBoundary()
    {
        var settings = new TestHostSettings
        {
            Host = "localhost",
            ConnectionLimit = 1,
            Port = 65_535,
            MaintenanceInterval = TimeSpan.FromTicks(1),
            QueueCleanupInterval = TimeSpan.FromTicks(1),
            MaintenanceBatchSize = 1,
        };

        Assert.Empty(settings.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "address-components-fail-during-validation")]
    public void Validate_ReportsInvalidAddressComponentsBeforeHostAddressFormatting()
    {
        var settings = new TestHostSettings
        {
            Host = "localhost",
            InstanceName = " ",
            VirtualHost = "invalid-host",
            Area = "9invalid",
        };

        ValidationResult[] failures = settings.Validate().ToArray();

        Assert.Equal(["InstanceName", "VirtualHost", "Area"], failures.Select(result => result.Key));
        Assert.All(failures, result => Assert.Equal(ValidationResultDisposition.Failure, result.Disposition));

        settings.InstanceName = "primary instance";
        settings.VirtualHost = "tenant_1";
        settings.Area = "billing_2";

        Assert.Empty(settings.Validate());
        Assert.Equal("db://localhost/tenant_1.billing_2?instance=primary%20instance", settings.HostAddress.AbsoluteUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "area-requires-a-named-virtual-host")]
    public void Validate_RejectsAnAreaWithoutANamedVirtualHost()
    {
        var settings = new TestHostSettings
        {
            Host = "localhost",
            VirtualHost = "/",
            Area = "billing",
        };

        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal("Area", failure.Key);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Throws<ArgumentException>(() => settings.HostAddress);
    }

    [Theory]
    [InlineData("/var/run/postgresql")]
    [InlineData("@transport")]
    [RequirementCoverage("REQ-VSB-SQL-HOST-SETTINGS", "unix-socket-hosts-cannot-bypass-validation")]
    public void Validate_RejectsUnixSocketHostsSetThroughThePublicConfigurator(string host)
    {
        var settings = new TestHostSettings
        {
            Host = host,
        };

        ValidationResult failure = Assert.Single(settings.Validate());

        Assert.Equal("Host", failure.Key);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Throws<ArgumentException>(() => settings.HostAddress);
    }

    private sealed class TestHostSettings : ConfigurationSqlHostSettings
    {
        public TestHostSettings()
        {
        }

        public TestHostSettings(Uri address)
            : base(address)
        {
        }

        public override ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration) =>
            throw new NotSupportedException();
    }
}
