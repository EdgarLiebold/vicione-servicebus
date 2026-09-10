using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Topology;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryTransportBoundaryTests
{
    private static readonly Uri HostAddress = new("loopback://localhost/tenant/");

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "host-address-canonical-contract")]
    public void HostAddress_ParsesAndRoundTripsItsLogicalIdentity()
    {
        var address = new InMemoryHostAddress(new Uri("loopback://LOCALHOST/tenant%20blue/"));

        Assert.Equal("loopback", address.Scheme);
        Assert.Equal("localhost", address.Host);
        Assert.Equal("tenant blue", address.VirtualHost);
        Assert.Equal(new Uri("loopback://localhost/tenant%20blue"), (Uri)address);
    }

    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("loopback://localhost:1234/")]
    [InlineData("loopback://user@localhost/")]
    [InlineData("loopback://localhost/?option=true")]
    [InlineData("loopback://localhost/#fragment")]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "host-address-rejects-unsupported-components")]
    public void HostAddress_RejectsEveryUnsupportedComponent(string value)
    {
        Assert.Throws<ArgumentException>(() => new InMemoryHostAddress(new Uri(value)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "host-address-required-input")]
    public void HostAddress_RejectsANullAddress()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new InMemoryHostAddress(null!));

        Assert.Equal("address", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "endpoint-address-short-schemes-and-routing-type")]
    public void EndpointAddress_NormalizesShortQueueExchangeAndTopicAddresses()
    {
        var queue = new InMemoryEndpointAddress(HostAddress, new Uri("queue:orders"));
        var exchange = new InMemoryEndpointAddress(
            HostAddress,
            new Uri("exchange:events?type=direct"));
        var topic = new InMemoryEndpointAddress(HostAddress, new Uri("topic:notifications"));

        Assert.Equal("orders", queue.Name);
        Assert.Equal(ExchangeType.FanOut, queue.ExchangeType);
        Assert.Equal(new Uri("loopback://localhost/tenant/orders"), (Uri)queue);

        Assert.Equal("events", exchange.Name);
        Assert.Equal(ExchangeType.Direct, exchange.ExchangeType);
        Assert.Equal(new Uri("loopback://localhost/tenant/events?type=Direct"), (Uri)exchange);

        Assert.Equal(ExchangeType.Topic, topic.ExchangeType);
        Assert.Equal(new Uri("loopback://localhost/tenant/notifications?type=Topic"), (Uri)topic);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-TRANSPORT-ISOLATION", "full-address-must-belong-to-provider-host")]
    public void EndpointAddress_RejectsAFullAddressOutsideTheConfiguredHostBoundary()
    {
        Assert.Throws<ArgumentException>(() => new InMemoryEndpointAddress(
            HostAddress,
            new Uri("loopback://other/tenant/orders")));
        Assert.Throws<ArgumentException>(() => new InMemoryEndpointAddress(
            HostAddress,
            new Uri("loopback://localhost/other/orders")));

        var matching = new InMemoryEndpointAddress(
            HostAddress,
            new Uri("loopback://localhost/tenant/orders?type=topic"));
        Assert.Equal("orders", matching.Name);
        Assert.Equal(ExchangeType.Topic, matching.ExchangeType);
    }

    [Theory]
    [InlineData("exchange:orders?unknown=true")]
    [InlineData("exchange:orders?bind=true")]
    [InlineData("exchange:orders?queue=orders.worker")]
    [InlineData("exchange:orders?type=invalid")]
    [InlineData("exchange:orders?type=direct&type=topic")]
    [InlineData("topic:orders?type=direct")]
    [InlineData("exchange:")]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "endpoint-address-rejects-malformed-options")]
    public void EndpointAddress_RejectsMalformedOrContradictoryAddresses(string value)
    {
        Assert.Throws<ArgumentException>(() => new InMemoryEndpointAddress(HostAddress, new Uri(value)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "endpoint-address-explicit-input-contract")]
    public void EndpointAddress_ValidatesEveryRequiredExplicitInput()
    {
        Assert.Equal("hostAddress", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryEndpointAddress(null!, "orders")).ParamName);
        Assert.Equal("exchangeName", Assert.Throws<ArgumentException>(() =>
            new InMemoryEndpointAddress(HostAddress, " ")).ParamName);
        Assert.Equal("exchangeType", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InMemoryEndpointAddress(HostAddress, "orders", exchangeType: (ExchangeType)42)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-ENDPOINT-CONCURRENCY", "delivery-sink-tracking-is-thread-safe")]
    public void DeliveryContext_TracksConcurrentSinkCompletionsWithoutLosingEntries()
    {
        var message = new InMemoryTransportMessage(
            Guid.Parse("ae0e8316-94de-47e2-9440-05c355f11330"),
            [],
            "application/json");
        var context = new InMemoryDeliveryContext(message, DateTimeOffset.UtcNow, CancellationToken.None);
        RecordingSink[] sinks = Enumerable.Range(0, 512).Select(_ => new RecordingSink()).ToArray();

        Parallel.ForEach(sinks, context.Delivered);

        Assert.All(sinks, sink => Assert.True(context.WasAlreadyDelivered(sink)));
        Assert.Equal("sink", Assert.Throws<ArgumentNullException>(() => context.Delivered(null!)).ParamName);
        Assert.Equal("sink", Assert.Throws<ArgumentNullException>(() => context.WasAlreadyDelivered(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "delivery-delay-preserves-clock-offset")]
    public void DeliveryContext_ResolvesDelayWithoutDiscardingTheClockOffset()
    {
        var now = new DateTimeOffset(2037, 4, 5, 6, 7, 8, TimeSpan.FromHours(2));
        var message = new InMemoryTransportMessage(
            Guid.Parse("8a32caeb-5737-454f-9a1b-d8e48647a155"),
            [],
            "application/json")
        {
            Delay = TimeSpan.FromMinutes(3),
        };

        var context = new InMemoryDeliveryContext(message, now, CancellationToken.None);

        Assert.Equal(now + TimeSpan.FromMinutes(3), context.EnqueueTime);
        Assert.Equal(TimeSpan.FromHours(2), context.EnqueueTime!.Value.Offset);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "send-transport-requires-host-configuration")]
    public void SendTransportContext_RejectsAMissingHostConfigurationBeforeOtherDependencies()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new InMemorySendTransportContext(null!, null!, null!, null!));

        Assert.Equal("hostConfiguration", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "publish-contract-discovery-and-explicit-registration")]
    public void PublishTopologyDiscovery_RegistersOnlyAcceptedContractsExactlyOnce()
    {
        var discovered = new List<Type>();
        var explicitTypes = new List<Type>();

        _ = InMemoryBus.Create(configurator =>
        {
            configurator.AddPublishMessageTypesFromNamespaceContaining<DiscoveryMessageOne>(
                (_, messageType) => discovered.Add(messageType),
                messageType => messageType == typeof(DiscoveryMessageOne) || messageType == typeof(DiscoveryMessageTwo));
            configurator.AddPublishMessageTypes(
                [typeof(ExplicitMessage)],
                (_, messageType) => explicitTypes.Add(messageType));
        });

        Assert.Equal(
            [typeof(DiscoveryMessageOne), typeof(DiscoveryMessageTwo)],
            discovered.OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Equal([typeof(ExplicitMessage)], explicitTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "publish-contract-registration-required-inputs")]
    public void PublishTopologyDiscovery_RejectsEveryMissingRequiredInput()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining<DiscoveryMessageOne>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            InMemoryPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining(null!, typeof(DiscoveryMessageOne))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            InMemoryPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining(
                CreateConfigurator(),
                null!)).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentNullException>(() =>
            InMemoryPublishTopologyConfigurationExtensions.AddPublishMessageTypes(CreateConfigurator(), null!)).ParamName);
        Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() =>
            InMemoryPublishTopologyConfigurationExtensions.AddPublishMessageTypes(CreateConfigurator(), [null!])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "consume-topology-contract-and-application")]
    public void ConsumeTopology_ValidatesInputsAndAppliesEveryConfiguredBinding()
    {
        IMessageTopology messageTopology = InMemoryBus.CreateMessageTopology();
        var publishTopology = new InMemoryPublishTopology(messageTopology);

        Assert.Equal("messageTopology", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryConsumeTopology(null!, publishTopology)).ParamName);
        Assert.Equal("publishTopology", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryConsumeTopology(messageTopology, null!)).ParamName);

        var topology = new InMemoryConsumeTopology(messageTopology, publishTopology);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            topology.AddSpecification(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() =>
            topology.Apply(null!)).ParamName);

        topology.Bind("source", ExchangeType.Direct, "tenant-a");
        topology.AddSpecification(new InvalidInMemoryConsumeTopologySpecification("binding", "invalid"));
        IInMemoryMessageConsumeTopologyConfigurator<DiscoveryMessageOne> configurable =
            ((IInMemoryConsumeTopologyConfigurator)topology).GetMessageTopology<DiscoveryMessageOne>();
        IInMemoryMessageConsumeTopology<DiscoveryMessageOne> readable =
            ((IInMemoryConsumeTopology)topology).GetMessageTopology<DiscoveryMessageOne>();
        configurable.Bind(ExchangeType.Topic, "events.*");

        var builder = new RecordingConsumeTopologyBuilder("destination", "input");
        topology.Apply(builder);

        Assert.Same(configurable, readable);
        Assert.Equal(2, builder.Declarations.Count);
        Assert.Contains(("source", ExchangeType.Direct), builder.Declarations);
        Assert.Contains(builder.Declarations, declaration => declaration.Type == ExchangeType.Topic);
        Assert.Equal(2, builder.ExchangeBindings.Count);
        Assert.Contains(("source", "destination", "tenant-a"), builder.ExchangeBindings);
        Assert.Contains(builder.ExchangeBindings, binding =>
            binding.Destination == "destination" && binding.RoutingKey == "events.*");
        ValidationResult failure = Assert.Single(topology.Validate());
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal("binding", failure.Key);
        Assert.Equal("invalid", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "consume-topology-specification-boundaries")]
    public void ConsumeTopologySpecifications_RejectInvalidInputsAndPreserveValidOperations()
    {
        Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
            new InvalidInMemoryConsumeTopologySpecification(" ", "invalid")).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentException>(() =>
            new InvalidInMemoryConsumeTopologySpecification("binding", " ")).ParamName);
        Assert.Equal("exchange", Assert.Throws<ArgumentException>(() =>
            new ExchangeBindingConsumeTopologySpecification(" ", ExchangeType.Direct, null)).ParamName);
        Assert.Equal("exchangeType", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExchangeBindingConsumeTopologySpecification("source", (ExchangeType)42, null)).ParamName);
        Assert.Equal("routingKey", Assert.Throws<ArgumentException>(() =>
            new ExchangeBindingConsumeTopologySpecification("source", ExchangeType.FanOut, "invalid")).ParamName);

        var invalid = new InvalidInMemoryConsumeTopologySpecification("binding", "invalid");
        var valid = new ExchangeBindingConsumeTopologySpecification("source", ExchangeType.Direct, "tenant-a");
        var builder = new RecordingConsumeTopologyBuilder("destination", "input");

        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => invalid.Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => valid.Apply(null!)).ParamName);
        invalid.Apply(builder);
        valid.Apply(builder);

        Assert.Equal(("source", ExchangeType.Direct), Assert.Single(builder.Declarations));
        Assert.Equal(("source", "destination", "tenant-a"), Assert.Single(builder.ExchangeBindings));
        Assert.Empty(builder.QueueBindings);
        Assert.Empty(valid.Validate());
        ValidationResult failure = Assert.Single(invalid.Validate());
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal("binding", failure.Key);
        Assert.Equal("invalid", failure.Message);
    }

    private static IInMemoryBusFactoryConfigurator CreateConfigurator()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var configuration = new InMemoryBusConfiguration(topology, HostAddress);
        return new InMemoryBusFactoryConfigurator(configuration);
    }

    public sealed record DiscoveryMessageOne;

    public sealed record DiscoveryMessageTwo;

    public sealed record ExplicitMessage;

    private sealed class RecordingSink : IMessageSink<InMemoryTransportMessage>
    {
        public Task DeliverAsync(DeliveryContext<InMemoryTransportMessage> context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingConsumeTopologyBuilder(string exchange, string queue) : IMessageFabricConsumeTopologyBuilder
    {
        public List<(string Name, ExchangeType Type)> Declarations { get; } = [];

        public List<(string Source, string Destination, string? RoutingKey)> ExchangeBindings { get; } = [];

        public List<(string Source, string Destination)> QueueBindings { get; } = [];

        public string Exchange { get; set; } = exchange;

        public string Queue { get; set; } = queue;

        public void ExchangeBind(string source, string destination, string? routingKey) =>
            ExchangeBindings.Add((source, destination, routingKey));

        public void QueueBind(string source, string destination) => QueueBindings.Add((source, destination));

        public void ExchangeDeclare(string name, ExchangeType exchangeType) =>
            Declarations.Add((name, exchangeType));

        public void QueueDeclare(string name)
        {
        }
    }
}
