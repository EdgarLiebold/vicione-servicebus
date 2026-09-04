using System.Text;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Operations;

public sealed class RabbitMqFaultRedriveTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "request-bounds-and-defaults")]
    public void Request_EnforcesDefaultsAbsoluteBoundsAndFilterShape()
    {
        var defaults = new RabbitMqFaultRedriveRequest("orders");

        Assert.Equal(100, RabbitMqFaultRedriveRequest.DefaultMaxMessages);
        Assert.Equal(1000, RabbitMqFaultRedriveRequest.DefaultMaxScanCount);
        Assert.Equal(1000, RabbitMqFaultRedriveRequest.AbsoluteMaxMessages);
        Assert.Equal(10000, RabbitMqFaultRedriveRequest.AbsoluteMaxScanCount);
        Assert.Equal((100, 1000), (defaults.MaxMessages, defaults.MaxScanCount));
        RabbitMqFaultRedriveLoop.Validate(defaults);

        Assert.Throws<ArgumentNullException>(() => new RabbitMqFaultRedriveRequest(null!));
        Assert.Throws<ArgumentException>(() => RabbitMqFaultRedriveLoop.Validate(new RabbitMqFaultRedriveRequest(" ")));
        Assert.Throws<ArgumentOutOfRangeException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { MaxMessages = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { MaxMessages = RabbitMqFaultRedriveRequest.AbsoluteMaxMessages + 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { MaxScanCount = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { MaxScanCount = RabbitMqFaultRedriveRequest.AbsoluteMaxScanCount + 1 }));
        Assert.Throws<ArgumentException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { MaxMessages = 2, MaxScanCount = 1 }));
        Assert.Throws<ArgumentException>(() => RabbitMqFaultRedriveLoop.Validate(
            defaults with { FaultExceptionType = "\t" }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "exact-structured-filter-conjunction")]
    public void Filters_RequireEveryExactIdentifierAndSupportBrokerHeaderRepresentations()
    {
        Guid messageId = Guid.Parse("0ed8a641-1ca2-459a-aaee-fad019538069");
        Guid correlationId = Guid.Parse("299ecf2b-9e12-4eca-8b9d-d152f9b94d6e");
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            FaultExceptionType = "System.InvalidOperationException",
        };
        var properties = Properties(messageId, correlationId, Encoding.UTF8.GetBytes(request.FaultExceptionType));

        Assert.True(RabbitMqFaultRedriveLoop.Matches(properties, request));
        properties.Headers![MessageHeaders.FaultExceptionType] =
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(request.FaultExceptionType));
        Assert.True(RabbitMqFaultRedriveLoop.Matches(properties, request));
        properties.Headers[MessageHeaders.FaultExceptionType] = request.FaultExceptionType;
        Assert.True(RabbitMqFaultRedriveLoop.Matches(properties, request));

        properties.MessageId = Guid.Parse("f59bd5d9-90a7-42c6-809b-a0e1f7d582a8").ToString("D");
        Assert.False(RabbitMqFaultRedriveLoop.Matches(properties, request));
        properties.MessageId = messageId.ToString("D");
        properties.CorrelationId = "not-a-guid";
        Assert.False(RabbitMqFaultRedriveLoop.Matches(properties, request));
        properties.CorrelationId = correlationId.ToString("D");
        properties.Headers[MessageHeaders.FaultExceptionType] = "system.invalidoperationexception";
        Assert.False(RabbitMqFaultRedriveLoop.Matches(properties, request));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "bounded-scan-and-redrive")]
    public async Task Loop_SkipsNonmatchesAndStopsExactlyAtTheRedriveLimitAsync()
    {
        Guid expected = Guid.Parse("23413ed8-f70b-4aca-bd41-939cc7b02752");
        var channel = new RecordingChannel(
            Delivery(1, messageId: Guid.Parse("e07f2aa8-a3c9-48ea-bb28-5a52872d5175")),
            Delivery(2, messageId: expected),
            Delivery(3, messageId: Guid.Parse("2449c7f9-957a-4fb3-879a-120247676b26")),
            Delivery(4, messageId: expected),
            Delivery(5, messageId: expected));
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 2,
            MaxScanCount = 4,
            MessageId = expected,
        };

        RabbitMqFaultRedriveResult result = await RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken);

        Assert.Equal((4, 2, 2), (result.Scanned, result.Matched, result.Redriven));
        Assert.False(result.SourceExhausted);
        Assert.False(result.ScanLimitReached);
        Assert.Equal(4, channel.GetCount);
        Assert.Equal([2UL, 4UL], channel.Acknowledged);
        Assert.Equal(["route-2", "route-4"], channel.Published.Select(message => message.RoutingKey));
        Assert.All(channel.Published, message => Assert.Equal("orders", message.ExchangeName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "scan-limit-and-unmatched-retention")]
    public async Task Loop_ReportsTheScanLimitWithoutAcknowledgingUnmatchedMessagesAsync()
    {
        Guid expected = Guid.Parse("25d7db48-d7d8-43a8-b5e4-3d5eb663867d");
        var channel = new RecordingChannel(
            Delivery(1),
            Delivery(2),
            Delivery(3),
            Delivery(4, messageId: expected));
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 3,
            MaxScanCount = 3,
            MessageId = expected,
        };

        RabbitMqFaultRedriveResult result = await RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken);

        Assert.Equal((3, 0, 0), (result.Scanned, result.Matched, result.Redriven));
        Assert.False(result.SourceExhausted);
        Assert.True(result.ScanLimitReached);
        Assert.Equal(3, channel.GetCount);
        Assert.Empty(channel.Published);
        Assert.Empty(channel.Acknowledged);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "source-exhaustion-result")]
    public async Task Loop_ReportsSourceExhaustionAfterTheLastAvailableMessageAsync()
    {
        var channel = new RecordingChannel(Delivery(7));
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 2,
            MaxScanCount = 2,
        };

        RabbitMqFaultRedriveResult result = await RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken);

        Assert.Equal((1, 1, 1), (result.Scanned, result.Matched, result.Redriven));
        Assert.True(result.SourceExhausted);
        Assert.False(result.ScanLimitReached);
        Assert.Equal([7UL], channel.Acknowledged);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "confirm-before-source-ack")]
    public async Task Loop_AcknowledgesOnlyAfterTheTargetPublishCompletesAsync()
    {
        var channel = new RecordingChannel(Delivery(9));
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 1,
            MaxScanCount = 1,
        };

        await RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken);

        Assert.Equal(["publish:start:9", "publish:confirmed:9", "ack:9"], channel.Operations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "publish-failure-retains-source")]
    public async Task PublishFailure_PropagatesWithoutAcknowledgingTheSourceAsync()
    {
        var expected = new IOException("mandatory publish rejected");
        var channel = new RecordingChannel(Delivery(11)) { PublishFailure = expected };
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 1,
            MaxScanCount = 1,
        };

        IOException actual = await Assert.ThrowsAsync<IOException>(() => RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["publish:start:11"], channel.Operations);
        Assert.Empty(channel.Acknowledged);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "post-confirm-ack-failure-is-at-least-once")]
    public async Task AckFailure_PropagatesAfterConfirmationAndLeavesTheAtLeastOnceWindowVisibleAsync()
    {
        var expected = new IOException("connection lost before source ack");
        var channel = new RecordingChannel(Delivery(13)) { AcknowledgeFailure = expected };
        var request = new RabbitMqFaultRedriveRequest("orders")
        {
            MaxMessages = 1,
            MaxScanCount = 1,
        };

        IOException actual = await Assert.ThrowsAsync<IOException>(() => RabbitMqFaultRedriveLoop.ExecuteAsync(
            channel,
            request,
            "orders_error",
            TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["publish:start:13", "publish:confirmed:13", "ack:13"], channel.Operations);
        Assert.Empty(channel.Acknowledged);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "dedicated-confirm-channel-options")]
    public void OperationsChannel_AlwaysEnablesConfirmationAndReturnTracking()
    {
        CreateChannelOptions options = RabbitMqFaultRedriveExecutor.CreateOperationsChannelOptions();

        Assert.True(options.PublisherConfirmationsEnabled);
        Assert.True(options.PublisherConfirmationTrackingEnabled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-FAULT-REDRIVE", "formatted-source-name-validation")]
    public void Operations_ValidateTheFormattedSourceQueueNameBeforeConnectionWork()
    {
        IRabbitMqTopologyConfiguration topology =
            new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());

        RabbitMqAddressException exception = Assert.Throws<RabbitMqAddressException>(() =>
            RabbitMqFaultRedriveExecutor.GetValidatedSourceQueueName(
                new RabbitMqFaultRedriveRequest(new string('q', 255)),
                topology.Send.EntityNameValidator,
                topology.Send.ErrorQueueNameFormatter));

        Assert.Contains("255 bytes", exception.Message, StringComparison.Ordinal);
    }

    private static RabbitMqFaultRedriveDelivery Delivery(ulong tag, Guid? messageId = null)
    {
        var properties = new BasicProperties();
        if (messageId.HasValue)
            properties.MessageId = messageId.Value.ToString("D");
        return new RabbitMqFaultRedriveDelivery(tag, $"route-{tag}", properties, new byte[] { checked((byte)tag) });
    }

    private static BasicProperties Properties(Guid messageId, Guid correlationId, object faultType) => new()
    {
        MessageId = messageId.ToString("D"),
        CorrelationId = correlationId.ToString("D"),
        Headers = new Dictionary<string, object?>
        {
            [MessageHeaders.FaultExceptionType] = faultType,
        },
    };

    private sealed class RecordingChannel(params RabbitMqFaultRedriveDelivery[] deliveries) : IRabbitMqFaultRedriveChannel
    {
        private readonly Queue<RabbitMqFaultRedriveDelivery> _deliveries = new(deliveries);

        public Exception? PublishFailure { get; init; }
        public Exception? AcknowledgeFailure { get; init; }
        public int GetCount { get; private set; }
        public List<PublishedMessage> Published { get; } = [];
        public List<ulong> Acknowledged { get; } = [];
        public List<string> Operations { get; } = [];

        public Task<RabbitMqFaultRedriveDelivery?> GetAsync(string queueName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("orders_error", queueName);
            GetCount++;
            return Task.FromResult(_deliveries.TryDequeue(out RabbitMqFaultRedriveDelivery? delivery) ? delivery : null);
        }

        public Task PublishAsync(
            string exchangeName,
            string routingKey,
            BasicProperties properties,
            ReadOnlyMemory<byte> body,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ulong tag = body.Span[0];
            Operations.Add($"publish:start:{tag}");
            if (PublishFailure != null)
                return Task.FromException(PublishFailure);

            Published.Add(new PublishedMessage(exchangeName, routingKey, properties, body.ToArray()));
            Operations.Add($"publish:confirmed:{tag}");
            return Task.CompletedTask;
        }

        public Task AcknowledgeAsync(ulong deliveryTag, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Operations.Add($"ack:{deliveryTag}");
            if (AcknowledgeFailure != null)
                return Task.FromException(AcknowledgeFailure);

            Acknowledged.Add(deliveryTag);
            return Task.CompletedTask;
        }
    }

    private sealed record PublishedMessage(
        string ExchangeName,
        string RoutingKey,
        BasicProperties Properties,
        byte[] Body);
}
