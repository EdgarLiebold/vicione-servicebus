using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport;

public sealed class RabbitMqAddressExtensionsTests
{
    [Theory]
    [InlineData("rabbitmq://some_server", 5672)]
    [InlineData("rabbitmq://some_server:12/", 12)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "host-settings-port-and-root-virtual-host")]
    public void HostSettings_PreserveTheCanonicalPortAndRootVirtualHost(string source, int expectedPort)
    {
        RabbitMqHostSettings settings = new Uri(source).GetHostSettings();

        Assert.Equal("some_server", settings.Host);
        Assert.Equal(expectedPort, settings.Port);
        Assert.Equal("/", settings.VirtualHost);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-SETTINGS", "full-address-projection")]
    public void FullAddress_ProjectsHostQueueAndEmptyCredentials()
    {
        var address = new Uri("rabbitmq://some_server/thehost/queue");

        RabbitMqHostSettings host = address.GetHostSettings();
        ReceiveSettings receive = address.GetReceiveSettings();

        Assert.Equal("some_server", host.Host);
        Assert.Equal("thehost", host.VirtualHost);
        Assert.Equal("", host.Username);
        Assert.Equal("", host.Password);
        Assert.Equal("queue", receive.QueueName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-SETTINGS", "default-virtual-host")]
    public void SinglePathSegment_UsesTheDefaultVirtualHostAndQueueName()
    {
        var hostAddress = new Uri("rabbitmq://some_server/");
        var endpointAddress = new Uri("rabbitmq://some_server/the_queue");

        Assert.Equal("/", hostAddress.GetHostSettings().VirtualHost);
        Assert.Equal("the_queue", endpointAddress.GetReceiveSettings().QueueName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-SETTINGS", "ttl-queue-argument")]
    public void TimeToLive_BecomesTheExactQueueArgument()
    {
        ReceiveSettings settings = new Uri("rabbitmq://localhost/vhost/orders?ttl=30000").GetReceiveSettings();

        Assert.Equal("orders", settings.QueueName);
        Assert.Single(settings.QueueArguments);
        Assert.Equal(30000, Assert.IsType<int>(settings.QueueArguments["x-message-ttl"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-SETTINGS", "prefetch-without-queue-argument")]
    public void Prefetch_ChangesOnlyTheReceiveLimit()
    {
        ReceiveSettings settings = new Uri("rabbitmq://localhost/vhost/orders?prefetch=32").GetReceiveSettings();

        Assert.Equal("orders", settings.QueueName);
        Assert.Equal((ushort)32, settings.PrefetchCount);
        Assert.Empty(settings.QueueArguments);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-SETTINGS", "temporary-queue-contract")]
    public void TemporaryWildcard_CreatesAnExclusiveAutoDeleteNonDurableQueue()
    {
        ReceiveSettings settings = new Uri("rabbitmq://localhost/vhost/*?temporary=true").GetReceiveSettings();

        Assert.NotEqual(Guid.Empty, Guid.Parse(settings.QueueName));
        Assert.True(settings.Exclusive);
        Assert.True(settings.AutoDelete);
        Assert.False(settings.Durable);
        Assert.Empty(settings.QueueArguments);
    }

    [Theory]
    [InlineData("rabbitmq://te%24t:Pa%24%24word@broker", "te$t", "Pa$$word")]
    [InlineData("rabbitmq://user:part1:part2@broker", "user", "part1:part2")]
    [InlineData("rabbitmq://user:pa+ss@broker", "user", "pa+ss")]
    [InlineData("rabbitmq://user@broker", "user", "")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CREDENTIALS", "decode-without-truncation")]
    public void Credentials_AreDecodedWithoutTruncatingThePassword(
        string source,
        string expectedUsername,
        string expectedPassword)
    {
        RabbitMqHostSettings settings = new Uri(source).GetHostSettings();

        Assert.Equal(expectedUsername, settings.Username);
        Assert.Equal(expectedPassword, settings.Password);
    }

    [Theory]
    [InlineData("amqps://broker:25671/production", true)]
    [InlineData("rabbitmqs://broker:25671/production", true)]
    [InlineData("rabbitmq://broker:5671/production", false)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "security-follows-scheme-not-port")]
    public void TransportSecurity_FollowsTheSchemeInsteadOfThePort(string source, bool expectedTls)
    {
        RabbitMqHostSettings settings = new Uri(source).GetHostSettings();

        Assert.Equal(expectedTls, settings.Ssl);
    }
}
