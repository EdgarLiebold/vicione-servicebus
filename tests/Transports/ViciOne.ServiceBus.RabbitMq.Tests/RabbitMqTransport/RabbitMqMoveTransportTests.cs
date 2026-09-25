using System.Globalization;
using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqMoveTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "missing-channel-rejected-before-topology")]
    public async Task DeadLetter_RejectsAReceiveContextWithoutAChannelBeforeBrokerWorkAsync()
    {
        var harness = new MoveHarness();
        ReceiveContext receive = harness.CreateReceiveContext(includeChannel: false);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Transport.SendAsync(receive, "skipped", TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.Empty(harness.DeclaredExchanges);
        Assert.Empty(harness.Publishes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "closed-channel-preserves-broker-reason")]
    public async Task DeadLetter_PreservesTheBrokerCloseReasonWithoutDeclaringOrPublishingAsync()
    {
        var reason = new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "precondition failed");
        var harness = new MoveHarness(isClosed: true, closeReason: reason);

        OperationInterruptedException exception = await Assert.ThrowsAsync<OperationInterruptedException>(() =>
            harness.Transport.SendAsync(harness.CreateReceiveContext(), "skipped", TestContext.Current.CancellationToken));

        Assert.Same(reason, exception.ShutdownReason);
        Assert.Empty(harness.DeclaredExchanges);
        Assert.Empty(harness.Publishes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "closed-channel-synthesizes-library-reason")]
    public async Task DeadLetter_SynthesizesALibraryReasonWhenTheBrokerProvidedNoneAsync()
    {
        var harness = new MoveHarness(isClosed: true);

        OperationInterruptedException exception = await Assert.ThrowsAsync<OperationInterruptedException>(() =>
            harness.Transport.SendAsync(harness.CreateReceiveContext(), "skipped", TestContext.Current.CancellationToken));

        ShutdownEventArgs synthesized = Assert.IsType<ShutdownEventArgs>(exception.ShutdownReason);
        Assert.Equal(ShutdownInitiator.Library, synthesized.Initiator);
        Assert.Equal(491, synthesized.ReplyCode);
        Assert.Contains("no longer available", synthesized.ReplyText, StringComparison.Ordinal);
        Assert.Empty(harness.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "delivery-copy-mandatory-publish-and-source-ownership")]
    public async Task DeadLetter_CopiesTheDeliveryAndPublishesMandatoryAfterTopologyAsync()
    {
        var harness = new MoveHarness();
        var incoming = new BasicProperties
        {
            MessageId = "source-id",
            Headers = new Dictionary<string, object?> { ["original"] = "preserved" },
        };
        ReceiveContext receive = harness.CreateReceiveContext("orders.created", incoming);

        await harness.Transport.SendAsync(receive, "skipped", TestContext.Current.CancellationToken);

        Assert.Equal(["declare", "publish"], harness.Events);
        Assert.Equal("dead", Assert.Single(harness.DeclaredExchanges));
        PublishedMessage publish = Assert.Single(harness.Publishes);
        Assert.Equal("dead", publish.Exchange);
        Assert.Equal("orders.created", publish.RoutingKey);
        Assert.True(publish.Mandatory);
        Assert.True(publish.AwaitAck);
        Assert.Equal(harness.Body, publish.Body);
        Assert.Equal(harness.ReceiveCancellationToken, publish.CancellationToken);
        Assert.Equal("source-id", publish.Properties.MessageId);
        Assert.Equal("preserved", publish.Properties.Headers!["original"]);
        Assert.Equal("skipped", publish.Properties.Headers[MessageHeaders.Reason]);
        Assert.Equal(HostMetadataCache.Host.MachineName, publish.Properties.Headers[MessageHeaders.Host.MachineName]);
        Assert.Equal(HostMetadataCache.Host.ProcessId.ToString(CultureInfo.InvariantCulture),
            publish.Properties.Headers[MessageHeaders.Host.ProcessId]);
        Assert.NotSame(incoming.Headers, publish.Properties.Headers);
        Assert.Equal("preserved", Assert.Single(incoming.Headers).Value);
        Assert.False(incoming.Headers.ContainsKey(MessageHeaders.Reason));
        Assert.False(incoming.Headers.ContainsKey(MessageHeaders.Host.MachineName));
        Assert.False(incoming.Headers.ContainsKey(MessageHeaders.Host.ProcessId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "copied-headers-retain-key-comparison")]
    public async Task DeadLetter_ReplacesACaseInsensitiveReasonWithoutMutatingTheSourceAsync()
    {
        var harness = new MoveHarness();
        var incoming = new BasicProperties
        {
            Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["vsb-reason"] = "old",
                ["original"] = "preserved",
            },
        };

        await harness.Transport.SendAsync(harness.CreateReceiveContext("orders.created", incoming), "skipped",
            TestContext.Current.CancellationToken);

        IDictionary<string, object?> movedHeaders = Assert.Single(harness.Publishes).Properties.Headers!;
        Assert.NotSame(incoming.Headers, movedHeaders);
        Assert.Equal("skipped", movedHeaders[MessageHeaders.Reason]);
        Assert.Equal(1, movedHeaders.Keys.Count(key => StringComparer.OrdinalIgnoreCase.Equals(key, MessageHeaders.Reason)));
        Assert.Equal("preserved", movedHeaders["original"]);
        Assert.Equal(2, incoming.Headers.Count);
        Assert.Equal("old", incoming.Headers["vsb-reason"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "non-rabbitmq-delivery-uses-unspecified-reason")]
    public async Task DeadLetter_UsesEmptyRoutingAndAnUnspecifiedReasonWithoutBrokerDeliveryMetadataAsync()
    {
        var harness = new MoveHarness();

        await harness.Transport.SendAsync(harness.CreateReceiveContext(), null!, TestContext.Current.CancellationToken);

        PublishedMessage publish = Assert.Single(harness.Publishes);
        Assert.Equal("", publish.RoutingKey);
        Assert.Equal(harness.Body, publish.Body);
        Assert.Equal("Unspecified", publish.Properties.Headers![MessageHeaders.Reason]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MOVE-TRANSPORT", "failed-publish-evicts-channel-topology")]
    public async Task DeadLetter_RetriesTopologyAfterPublishFailureAndPreservesTheCauseAsync()
    {
        var harness = new MoveHarness();
        var failure = new IOException("publish rejected");
        harness.PublishFailure = failure;
        ReceiveContext receive = harness.CreateReceiveContext();

        IOException observed = await Assert.ThrowsAsync<IOException>(() =>
            harness.Transport.SendAsync(receive, "skipped", TestContext.Current.CancellationToken));

        Assert.Same(failure, observed);
        Assert.Equal(["declare", "publish"], harness.Events);

        harness.PublishFailure = null;
        await harness.Transport.SendAsync(receive, "retry", TestContext.Current.CancellationToken);

        Assert.Equal(["declare", "publish", "declare", "publish"], harness.Events);
        Assert.Equal(2, harness.DeclaredExchanges.Count);
        Assert.Equal(2, harness.Publishes.Count);
        Assert.Equal("", harness.Publishes[1].RoutingKey);
        Assert.Equal("retry", harness.Publishes[1].Properties.Headers![MessageHeaders.Reason]);

        await harness.Transport.SendAsync(receive, "third", TestContext.Current.CancellationToken);

        Assert.Equal(["declare", "publish", "declare", "publish", "publish"], harness.Events);
        Assert.Equal(2, harness.DeclaredExchanges.Count);
        Assert.Equal("third", harness.Publishes[2].Properties.Headers![MessageHeaders.Reason]);
    }

    private sealed class MoveHarness
    {
        private readonly PayloadStore _payloads = new();
        private readonly ChannelContext _channelContext;
        private readonly RabbitMqTopologyEntityCache _topologyCache = new();
        private readonly IReadOnlyBasicProperties _defaultProperties = new BasicProperties();

        public MoveHarness(bool isClosed = false, ShutdownEventArgs? closeReason = null)
        {
            IChannel channel = Proxy<IChannel>((method, _) => method.Name switch
            {
                "get_IsClosed" => isClosed,
                "get_CloseReason" => closeReason,
                _ => throw Unexpected(method),
            });
            ConnectionContext connection = Proxy<ConnectionContext>((method, _) => method.Name switch
            {
                "get_TopologyEntityCache" => _topologyCache,
                _ => throw Unexpected(method),
            });
            _channelContext = Proxy<ChannelContext>((method, args) => method.Name switch
            {
                "get_Channel" => channel,
                "get_ConnectionContext" => connection,
                "get_CancellationToken" => CancellationToken.None,
                "ExchangeDeclareAsync" => DeclareExchangeAsync(args!),
                "BasicPublishAsync" => PublishAsync(args!),
                _ when typeof(PipeContext).IsAssignableFrom(method.DeclaringType) => method.Invoke(_payloads, args),
                _ => throw Unexpected(method),
            });

            var builder = new PublishEndpointBrokerTopologyBuilder();
            builder.ExchangeDeclare("dead", ExchangeType.Fanout, durable: false, autoDelete: true,
                new Dictionary<string, object?>());
            var settings = Proxy<DeadLetterSettings>((method, _) => throw Unexpected(method));
            var filter = new ConfigureRabbitMqTopologyFilter<DeadLetterSettings>(settings, builder.BuildBrokerTopology());
            Transport = new RabbitMqDeadLetterTransport("dead", filter);
        }

        public readonly byte[] Body = [3, 5, 8, 13];
        public readonly List<string> Events = [];
        public readonly List<string> DeclaredExchanges = [];
        public readonly List<PublishedMessage> Publishes = [];
        public readonly CancellationToken ReceiveCancellationToken = new CancellationTokenSource().Token;
        public Exception? PublishFailure { get; set; }
        public RabbitMqDeadLetterTransport Transport { get; }

        public ReceiveContext CreateReceiveContext(string? routingKey = null, IReadOnlyBasicProperties? properties = null,
            bool includeChannel = true)
        {
            RabbitMqBasicConsumeContext? delivery = routingKey is null ? null :
                Proxy<RabbitMqBasicConsumeContext>((method, _) => method.Name switch
                {
                    "get_RoutingKey" => routingKey,
                    "get_Properties" => properties ?? _defaultProperties,
                    _ => throw Unexpected(method),
                });
            var body = new BinaryMessageBody(Body);
            return Proxy<ReceiveContext>((method, args) => method.Name switch
            {
                "TryGetPayload" => SupplyPayload(method, args!, delivery, includeChannel),
                "get_Body" => body,
                "get_CancellationToken" => ReceiveCancellationToken,
                _ => throw Unexpected(method),
            });
        }

        private bool SupplyPayload(MethodInfo method, object?[] args, RabbitMqBasicConsumeContext? delivery, bool includeChannel)
        {
            Type requested = method.GetGenericArguments()[0];
            object? payload = requested == typeof(ChannelContext) && includeChannel ? _channelContext :
                requested == typeof(RabbitMqBasicConsumeContext) ? delivery : null;
            args[0] = payload;
            return payload is not null;
        }

        private Task DeclareExchangeAsync(object?[] args)
        {
            Events.Add("declare");
            string exchange = (string)args[0]!;
            Assert.Equal(ExchangeType.Fanout, args[1]);
            Assert.Equal(false, args[2]);
            Assert.Equal(true, args[3]);
            DeclaredExchanges.Add(exchange);
            return Task.CompletedTask;
        }

        private Task PublishAsync(object?[] args)
        {
            Events.Add("publish");
            Publishes.Add(new PublishedMessage((string)args[0]!, (string)args[1]!, (bool)args[2]!,
                (BasicProperties)args[3]!, (byte[])args[4]!, (bool)args[5]!, (CancellationToken)args[6]!));
            return PublishFailure is { } failure ? Task.FromException(failure) : Task.CompletedTask;
        }

        private sealed class PayloadStore : BasePipeContext;
    }

    private sealed record PublishedMessage(string Exchange, string RoutingKey, bool Mandatory, BasicProperties Properties,
        byte[] Body, bool AwaitAck, CancellationToken CancellationToken);

    private static Exception Unexpected(MethodInfo method) => new NotSupportedException($"Unexpected call: {method.Name}");

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        T result = DispatchProxy.Create<T, StrictProxy<T>>();
        ((StrictProxy<T>)(object)result).Handler = handler;
        return result;
    }

    private class StrictProxy<T> : DispatchProxy where T : class
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("No proxy method was supplied."), args);
    }
}
