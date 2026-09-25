using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqDurableSendDispatcherBoundaryTests
{
    private static readonly Uri HostAddress = new("rabbitmq://localhost/test");
    private static readonly MessageContractIdentity ContractIdentity = new("vicione.tests.rabbitmq-durable-boundary", 1);

    [Theory]
    [InlineData("queue-declaration")]
    [InlineData("queue-name-without-binding")]
    [InlineData("non-durable")]
    [InlineData("auto-delete")]
    [InlineData("non-fanout")]
    [InlineData("alternate-exchange")]
    [InlineData("additional-binding")]
    [InlineData("direct-reply-to")]
    [InlineData("direct-reply-to-prefix")]
    [InlineData("short-exchange-address")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "unsafe-destination-rejected-before-endpoint-resolution")]
    public async Task UnsafeDestination_IsRejectedBeforeEndpointResolutionAsync(string option)
    {
        IBus bus = CreateBus(out RecordingBusProxy busProbe);
        var dispatcher = CreateDispatcher(bus);
        Uri destination = option switch
        {
            "queue-declaration" => new RabbitMqEndpointAddress(HostAddress, "orders", bindToQueue: true),
            "queue-name-without-binding" => new RabbitMqEndpointAddress(HostAddress, "orders", queueName: "other"),
            "non-durable" => new RabbitMqEndpointAddress(HostAddress, "orders", durable: false),
            "auto-delete" => new RabbitMqEndpointAddress(HostAddress, "orders", autoDelete: true),
            "non-fanout" => new RabbitMqEndpointAddress(HostAddress, "orders", exchangeType: RabbitMQ.Client.ExchangeType.Direct),
            "alternate-exchange" => new RabbitMqEndpointAddress(HostAddress, "orders", alternateExchange: "fallback"),
            "additional-binding" => new RabbitMqEndpointAddress(HostAddress, "orders", bindExchanges: ["source"]),
            "direct-reply-to" => new RabbitMqEndpointAddress(HostAddress, RabbitMqExchangeNames.ReplyTo),
            "direct-reply-to-prefix" => new RabbitMqEndpointAddress(HostAddress, RabbitMqExchangeNames.ReplyTo + ".client"),
            "short-exchange-address" => new Uri("exchange:orders"),
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            dispatcher.DispatchAsync(CreateContext(destination), TestContext.Current.CancellationToken));

        Assert.Contains("durable transport acceptance", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, busProbe.EndpointResolutionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "safe-destination-reaches-endpoint-resolution")]
    public async Task ExistingQueueExchange_ReachesEndpointResolutionAsync()
    {
        IBus bus = CreateBus(out RecordingBusProxy busProbe);
        var dispatcher = CreateDispatcher(bus);
        Uri destination = new RabbitMqEndpointAddress(HostAddress, "orders");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(CreateContext(destination), TestContext.Current.CancellationToken));

        Assert.Same(busProbe.EndpointResolutionFailure, exception);
        Assert.Equal(1, busProbe.EndpointResolutionCount);
        Assert.Equal(destination, busProbe.LastDestination);
    }

    private static RabbitMqDurableSendDispatcher<IBus> CreateDispatcher(IBus bus) =>
        new(bus, [new MessageContractCatalogBuilder().Register<DurableMessage>(ContractIdentity.Name).Build()]);

    private static IBus CreateBus(out RecordingBusProxy probe)
    {
        IBus bus = DispatchProxy.Create<IBus, RecordingBusProxy>();
        probe = (RecordingBusProxy)(object)bus;
        return bus;
    }

    private static DurableSendDispatchContext CreateContext(Uri destination)
    {
        DurableSendId id = new(Guid.NewGuid());
        return new DurableSendDispatchContext(
            new SerializedDurableSend
            {
                Id = id,
                ContractIdentity = ContractIdentity,
                DestinationAddress = destination,
                ContentType = MediaTypeNames.Application.Json,
                Body = new byte[] { 42 },
            },
            id,
            attempt: 1,
            new UnusedConsumerCompletion(id));
    }

    private sealed record DurableMessage;

    private sealed class UnusedConsumerCompletion(DurableSendId id) : IDurableSendConsumerCompletion
    {
        public DurableSendId DurableSendId => id;

        public ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromException<bool>(new InvalidOperationException("Broker acceptance must not use consumer completion."));
    }

    private class RecordingBusProxy : DispatchProxy
    {
        public int EndpointResolutionCount { get; private set; }
        public Uri? LastDestination { get; private set; }
        public InvalidOperationException EndpointResolutionFailure { get; } = new("Endpoint resolution reached.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Address" => HostAddress,
                nameof(IBus.GetSendEndpointAsync) => ResolveEndpoint(args),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }

        private Task<ISendEndpoint> ResolveEndpoint(object?[]? args)
        {
            EndpointResolutionCount++;
            LastDestination = Assert.IsType<Uri>(args![0]);
            return Task.FromException<ISendEndpoint>(EndpointResolutionFailure);
        }
    }
}
