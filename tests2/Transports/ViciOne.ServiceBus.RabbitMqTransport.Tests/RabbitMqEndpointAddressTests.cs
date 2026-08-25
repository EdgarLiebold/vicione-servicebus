using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests;

public sealed class RabbitMqEndpointAddressTests
{
    private static readonly Uri HostAddress = new("rabbitmq://localhost/test");

    [Theory]
    [InlineData("rabbitmq://remote/input-queue", "/", "input-queue", "rabbitmq://remote/input-queue")]
    [InlineData("rabbitmq://remote/production/client/input-queue", "production/client", "input-queue", "rabbitmq://remote/production%2Fclient/input-queue")]
    [InlineData("rabbitmq://remote/production%2Fclient/input-queue", "production/client", "input-queue", "rabbitmq://remote/production%2Fclient/input-queue")]
    [InlineData("rabbitmq://remote/vhost/the:queue", "vhost", "the:queue", "rabbitmq://remote/vhost/the:queue")]
    [InlineData("rabbitmq://remote/vhost/the.queue", "vhost", "the.queue", "rabbitmq://remote/vhost/the.queue")]
    [InlineData("rabbitmq://remote/vhost/the_queue", "vhost", "the_queue", "rabbitmq://remote/vhost/the_queue")]
    [InlineData("rabbitmq://remote/vhost/ßäöüÄÖÜ1234abc", "vhost", "ßäöüÄÖÜ1234abc", "rabbitmq://remote/vhost/ßäöüÄÖÜ1234abc")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "full-address-parsing-and-rendering")]
    public void FullAddresses_ParseAndRenderCanonically(
        string source,
        string expectedVirtualHost,
        string expectedName,
        string expectedAddress)
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(expectedVirtualHost, address.VirtualHost);
        Assert.Equal(expectedName, address.Name);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("queue:input-queue", true, "rabbitmq://localhost/test/input-queue?bind=true")]
    [InlineData("exchange:input-queue", false, "rabbitmq://localhost/test/input-queue")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "short-address-resolution")]
    public void ShortAddresses_ResolveAgainstTheHost(string source, bool expectedBindToQueue, string expectedAddress)
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(expectedBindToQueue, address.BindToQueue);
        Assert.Equal("input-queue", address.Name);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("rabbitmq://remote:5672/input", 5672, "rabbitmq://remote/input")]
    [InlineData("rabbitmq://remote:25672/input", 25672, "rabbitmq://remote:25672/input")]
    [InlineData("rabbitmqs://remote:5671/input", 5671, "rabbitmqs://remote/input")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "port-rendering")]
    public void EndpointPorts_ArePreservedAndRenderedCanonically(string source, int expectedPort, string expectedAddress)
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(expectedPort, address.Port);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "complete-roundtrip")]
    public void EndpointOptions_RoundTripWithoutLosingNamesOrFlags()
    {
        var source = new Uri(
            "exchange:orders?durable=false&autodelete=true&type=topic&bind=true&queue=orders.queue" +
            "&alternateexchange=alternate.ä&bindexchange=input.one&bindexchange=input.two&singleactiveconsumer=true");

        var address = new RabbitMqEndpointAddress(HostAddress, source);
        var roundTripped = new RabbitMqEndpointAddress(HostAddress, address.ToShortAddress());

        Assert.False(roundTripped.Durable);
        Assert.True(roundTripped.AutoDelete);
        Assert.Equal("topic", roundTripped.ExchangeType);
        Assert.True(roundTripped.BindToQueue);
        Assert.Equal("orders.queue", roundTripped.QueueName);
        Assert.Equal("alternate.ä", roundTripped.AlternateExchange);
        Assert.Equal(["input.one", "input.two"], roundTripped.BindExchanges);
        Assert.True(roundTripped.SingleActiveConsumer);
    }

    [Theory]
    [InlineData("exchange:orders?delayedtype=topic")]
    [InlineData("exchange:orders?type=direct&delayedtype=topic")]
    [InlineData("exchange:orders?delayedtype=topic&type=direct")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "delayed-exchange-roundtrip")]
    public void DelayedType_SelectsTheDelayedExchangeIndependentOfQueryOrder(string source)
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(RabbitMqEndpointAddress.DelayedMessageExchangeType, address.ExchangeType);
        Assert.Equal("topic", address.DelayedType);
        Assert.Equal(new Uri("exchange:orders?delayedtype=topic"), address.ToShortAddress());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "direct-delayed-exchange-normalization")]
    public void DirectConstruction_NormalizesADelayedExchange()
    {
        var address = new RabbitMqEndpointAddress(HostAddress, "orders", delayedType: "topic");

        Assert.Equal(RabbitMqEndpointAddress.DelayedMessageExchangeType, address.ExchangeType);
        Assert.Equal("topic", address.DelayedType);
        Assert.Equal(new Uri("exchange:orders?delayedtype=topic"), address.ToShortAddress());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "direct-construction-snapshot")]
    public void DirectConstruction_OwnsItsBindingSnapshot()
    {
        var bindings = new[] { "input.one", "input.two" };
        var address = new RabbitMqEndpointAddress(
            HostAddress,
            "orders",
            bindExchanges: bindings,
            alternateExchange: "alternate");

        bindings[0] = "mutated";

        Assert.Equal(["input.one", "input.two"], address.BindExchanges);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)address.BindExchanges)[0] = "mutated");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "direct-construction-validation")]
    public void DirectConstruction_ValidatesEntityNames()
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, "invalid/name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "explicit-empty-exchange-type")]
    public void DirectConstruction_RejectsAnExplicitEmptyExchangeType(string exchangeType)
    {
        Assert.Throws<ArgumentException>(() => new RabbitMqEndpointAddress(HostAddress, "orders", exchangeType));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "explicit-empty-delayed-type")]
    public void DirectConstruction_RejectsAnExplicitEmptyDelayedType(string delayedType)
    {
        Assert.Throws<ArgumentException>(() =>
            new RabbitMqEndpointAddress(HostAddress, "orders", delayedType: delayedType));
    }

    [Theory]
    [InlineData("exchange:orders?temporary=maybe")]
    [InlineData("exchange:orders?queue=")]
    [InlineData("exchange:orders?queue=invalid%2Fname")]
    [InlineData("exchange:orders?unknown=true")]
    [InlineData("exchange:orders?bind=true&bind=false")]
    [InlineData("exchange:invalid%2Fname")]
    [InlineData("exchange:orders?temporary=true&durable=false")]
    [InlineData("exchange:orders?durable=false&temporary=true")]
    [InlineData("exchange:orders?autodelete=true&temporary=true")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "invalid-or-ambiguous-values-fail-fast")]
    public void InvalidOrAmbiguousEndpointInputs_AreRejected(string source)
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, new Uri(source)));
    }

    [Theory]
    [InlineData("exchange:orders?heartbeat=30")]
    [InlineData("queue:orders?prefetch=16")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-OPTIONS", "short-address-rejects-host-options")]
    public void ShortAddresses_RejectHostOptionsThatCannotTakeEffect(string source)
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, new Uri(source)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENTITY-NAME", "generated-delay-name-validation")]
    public void DelaySettings_RejectAnAuxiliaryExchangeNameBeyondTheBrokerLimit()
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new string('a', 255));

        Assert.Throws<RabbitMqAddressException>(() => address.GetDelaySettings());
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("invalid/name")]
    [InlineData("invalid name")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENTITY-NAME", "invalid-characters")]
    public void InvalidEntityNames_AreRejected(string name)
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, name));
    }

    [Theory]
    [InlineData(255, 'a')]
    [InlineData(127, 'ä')]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENTITY-NAME", "utf8-byte-limit-accepted")]
    public void EntityNameLimit_AcceptsAtMost255Utf8Bytes(int length, char value)
    {
        var name = new string(value, length);

        Assert.Equal(name, new RabbitMqEndpointAddress(HostAddress, name).Name);
    }

    [Theory]
    [InlineData(256, 'a')]
    [InlineData(128, 'ä')]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENTITY-NAME", "utf8-byte-limit-rejected")]
    public void EntityNameLimit_RejectsMoreThan255Utf8Bytes(int length, char value)
    {
        var name = new string(value, length);

        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, name));
    }

    [Theory]
    [InlineData("exchange:orders", "exchange:orders")]
    [InlineData("queue:orders", "queue:orders")]
    [InlineData("rabbitmq://localhost/test/orders?bind=true&queue=orders.queue", "queue:orders?queue=orders.queue")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENDPOINT-ADDRESS", "canonical-short-form")]
    public void EndpointAddress_UsesTheCanonicalShortForm(string source, string expected)
    {
        var address = new RabbitMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(new Uri(expected), address.ToShortAddress());
    }
}
