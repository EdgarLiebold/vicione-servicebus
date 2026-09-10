using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Address;

public sealed class SqlAddressTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0001", "native-owner")]
    public void HostAddress_ParsesVirtualHost()
    {
        var uri = new Uri("db://localhost/customer_a");
        var address = new SqlHostAddress(uri);

        Assert.Equal("db", address.Scheme);
        Assert.Equal("localhost", address.Host);
        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Null(address.Area);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0002", "native-owner")]
    public void HostAddress_ParsesVirtualHostAndArea()
    {
        var uri = new Uri("db://localhost/customer_a.billing");
        var address = new SqlHostAddress(uri);

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal("billing", address.Area);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0003", "native-owner")]
    public void HostAddress_NormalizesTrailingSlashAfterArea()
    {
        var address = new SqlHostAddress(new Uri("db://localhost/customer_a.billing/"));

        Assert.Equal(new Uri("db://localhost/customer_a.billing"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0004", "native-owner")]
    public void HostAddress_DropsTrailingEntitySegment()
    {
        var address = new SqlHostAddress(new Uri("db://localhost/customer_a.billing/input-queue"));

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal("billing", address.Area);
        Assert.Equal(new Uri("db://localhost/customer_a.billing"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0005", "native-owner")]
    public void HostAddress_DefaultsBareAuthorityVirtualHost()
    {
        var uri = new Uri("db://localhost");
        var address = new SqlHostAddress(uri);

        Assert.Equal("/", address.VirtualHost);
        Assert.Null(address.Area);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0006", "native-owner")]
    public void HostAddress_DefaultsSlashOnlyVirtualHost()
    {
        var uri = new Uri("db://localhost/");
        var address = new SqlHostAddress(uri);

        Assert.Equal("/", address.VirtualHost);
        Assert.Null(address.Area);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0007", "native-owner")]
    public void HostAddress_RejectsAreaStartingWithDigit()
    {
        Assert.Throws<SqlEndpointAddressException>(
            () => new SqlHostAddress(new Uri("db://localhost/customer.16/input-queue")));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0008", "native-owner")]
    public void HostAddress_RejectsVirtualHostContainingHyphen()
    {
        Assert.Throws<SqlEndpointAddressException>(
            () => new SqlHostAddress(new Uri("db://localhost/customer-a.billing/input-queue")));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0009", "native-owner")]
    public void HostAddress_ParsesAndProjectsInstanceName()
    {
        var expected = new Uri("db://localhost/customer_a?instance=instance");
        var address = new SqlHostAddress(expected);

        Assert.Equal("instance", address.InstanceName);
        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal(expected, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0010", "native-owner")]
    public void EndpointAddress_ParsesEntityWithinVirtualHost()
    {
        var uri = new Uri("db://localhost/customer_a/input-queue");
        var address = new SqlEndpointAddress(new SqlHostAddress(new Uri("db://localhost/customer_a")), uri);

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0011", "native-owner")]
    public void EndpointAddress_ParsesVirtualHostAreaAndEntity()
    {
        var uri = new Uri("db://localhost/customer_a.billing/input-queue");
        var address = new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer_a.billing")), uri);

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal("billing", address.Area);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0012", "native-owner")]
    public void EndpointAddress_NormalizesTrailingSlashOnHost()
    {
        var expected = new Uri("db://localhost/customer_a.billing/input-queue");
        var address = new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer_a.billing/")), expected);

        Assert.Equal(expected, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0013", "native-owner")]
    public void EndpointAddress_ResolvesShortQueueAddress()
    {
        var address = new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer_a.billing/")),
            new Uri("queue:input-queue"));

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Equal("billing", address.Area);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(new Uri("db://localhost/customer_a.billing/input-queue"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0014", "native-owner")]
    public void EndpointAddress_ResolvesShortTopicAddress()
    {
        var address = new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer_a.billing/")),
            new Uri("topic:namespace:type"));

        Assert.Equal("customer_a", address.VirtualHost);
        Assert.Null(address.Area);
        Assert.Equal("namespace:type", address.Name);
        Assert.Equal(SqlEndpointKind.Topic, address.Kind);
        Assert.Equal(new Uri("db://localhost/customer_a/namespace:type?kind=topic"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0015", "native-owner")]
    public void EndpointAddress_PreservesIpv6HostAndPort()
    {
        var address = new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://[::1]:1433/")), new Uri("queue:input-queue"));

        Assert.Equal("[::1]", address.Host);
        Assert.Equal(1433, address.Port);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(new Uri("db://[::1]:1433/input-queue"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0016", "native-owner")]
    public void EndpointAddress_ProjectsSqlServerInstanceAsQueryOption()
    {
        var host = new SqlHostAddress("localhost", "instance", null, "customer_a", "billing");
        var address = new SqlEndpointAddress(host, new Uri("queue:input-queue"));

        Assert.Equal("instance", address.InstanceName);
        Assert.Equal("billing", address.Area);
        Assert.Equal(new Uri("db://localhost/customer_a.billing/input-queue?instance=instance"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0017", "native-owner")]
    public void EndpointAddress_RejectsMissingEntityName()
    {
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost")), new Uri("db://localhost")));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0018", "native-owner")]
    public void EndpointAddress_ParsesEntityWithDefaultVirtualHost()
    {
        var uri = new Uri("db://localhost/input-queue");
        var address = new SqlEndpointAddress(new SqlHostAddress(new Uri("db://localhost")), uri);

        Assert.Equal("/", address.VirtualHost);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(uri, (Uri)address);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0019", "native-owner")]
    public void EndpointAddress_RejectsInvalidArea()
    {
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer.16")),
            new Uri("db://localhost/customer.16/input-queue")));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0020", "native-owner")]
    public void EndpointAddress_RejectsInvalidVirtualHost()
    {
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(
            new SqlHostAddress(new Uri("db://localhost/customer-a.billing")),
            new Uri("db://localhost/customer-a.billing/input-queue")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-ADDRESS-VALIDATION", "host-components-reject-invalid-public-input")]
    public void HostAddress_RejectsInvalidPublicComponents()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlHostAddress(null!));
        Assert.Throws<ArgumentException>(() => new SqlHostAddress(new Uri("relative", UriKind.Relative)));
        Assert.Throws<ArgumentException>(() => new SqlHostAddress(" ", null, null, "transport", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlHostAddress("localhost", null, 0, "transport", null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlHostAddress("localhost", null, 65536, "transport", null));
        Assert.Throws<ArgumentException>(() => new SqlHostAddress("localhost", " ", null, "transport", null));
        Assert.Throws<ArgumentException>(() => new SqlHostAddress("localhost", null, null, "invalid-host", null));
        Assert.Throws<ArgumentException>(() => new SqlHostAddress("localhost", null, null, "transport", "9invalid"));
        Assert.Throws<InvalidOperationException>(() => _ = (Uri)default(SqlHostAddress));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-ADDRESS-VALIDATION", "endpoint-components-reject-invalid-public-input")]
    public void EndpointAddress_RejectsInvalidPublicComponents()
    {
        var host = new Uri("db://localhost/transport");

        Assert.Throws<ArgumentNullException>(() => new SqlEndpointAddress(null!, "orders"));
        Assert.Throws<ArgumentNullException>(() => new SqlEndpointAddress(host, (Uri)null!));
        Assert.Throws<ArgumentException>(() => new SqlEndpointAddress(host, " "));
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(host, "orders/path"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlEndpointAddress(host, "orders", kind: (SqlEndpointKind)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlEndpointAddress(host, "orders", TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => new SqlEndpointAddress(host, "events", TimeSpan.FromMinutes(1), SqlEndpointKind.Topic));
        Assert.Throws<InvalidOperationException>(() => _ = (Uri)default(SqlEndpointAddress));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-ADDRESS-VALIDATION", "query-values-are-lossless-and-invalid-values-fail")]
    public void EndpointAddress_RoundTripsValidQueryValuesAndRejectsInvalidOnes()
    {
        var host = new SqlHostAddress("localhost", "east instance", null, "transport", null);
        var endpoint = new SqlEndpointAddress(host, "orders", TimeSpan.FromMilliseconds(1500));

        Uri uri = endpoint;
        var parsed = new SqlEndpointAddress(host, uri);

        Assert.Equal(TimeSpan.FromMilliseconds(1500), parsed.AutoDeleteOnIdle);
        Assert.Equal("east instance", parsed.InstanceName);
        Assert.Contains("autodelete=1.5", uri.Query, StringComparison.Ordinal);
        Assert.Contains("instance=east%20instance", uri.Query, StringComparison.OrdinalIgnoreCase);

        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(host, new Uri("db://localhost/transport/orders?autodelete=-1")));
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(host, new Uri("db://localhost/transport/orders?autodelete=never")));
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(host, new Uri("db://localhost/transport/events?kind=unknown")));
        Assert.Throws<SqlEndpointAddressException>(() => new SqlEndpointAddress(
            host,
            new Uri("db://localhost/transport/events?kind=topic&autodelete=60")));
    }
}
