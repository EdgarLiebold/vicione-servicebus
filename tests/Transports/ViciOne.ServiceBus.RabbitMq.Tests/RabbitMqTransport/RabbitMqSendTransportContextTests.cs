using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mime;
using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqSendTransportContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "supported-header-wire-projection")]
    public async Task SendAsync_ProjectsSupportedHeadersWithoutOverwritingNativeValuesAsync()
    {
        DateTimeOffset timestamp = new(2026, 9, 21, 12, 34, 56, TimeSpan.Zero);
        DateTime localTimestamp = timestamp.LocalDateTime;
        Guid identifier = Guid.Parse("63d4ad9f-e55a-42fc-b50f-55bb5ad840bd");
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.BasicProperties.Headers = new Dictionary<string, object?> { ["existing"] = "native" };
        context.Headers.Set("existing", "application");
        context.Headers.Set("offset", timestamp);
        context.Headers.Set("local", localTimestamp);
        context.Headers.Set("guid", identifier);
        context.Headers.Set("CC", "copy@example.test");
        context.Headers.Set("BCC", new[] { "first@example.test", "second@example.test" });
        context.Headers.Set("uri", new Uri("https://example.test/resource"));
        context.Headers.Set("text", "value");
        context.Headers.Set("true", true);
        context.Headers.Set("false", false);
        context.Headers.Set("decimal", 12.5m);
        context.Headers.Set("formattable", new InvariantFormattable(12.5m));
        context.Headers.Set("unsupported", new object());
        context.Headers.Set(RabbitMqHeaders.Exchange, "ignored");
        context.Headers.Set(RabbitMqHeaders.RoutingKey, "ignored");
        context.Headers.Set(RabbitMqHeaders.DeliveryTag, 12UL);
        context.Headers.Set(RabbitMqHeaders.ConsumerTag, "ignored");
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        BasicProperties published = Assert.Single(channel.Published).BasicProperties;
        IDictionary<string, object?> headers = Assert.IsAssignableFrom<IDictionary<string, object?>>(published.Headers);
        Assert.Equal("native", headers["existing"]);
        Assert.Equal(timestamp.ToUnixTimeSeconds(), Assert.IsType<AmqpTimestamp>(headers["offset"]).UnixTime);
        Assert.Equal(new DateTimeOffset(localTimestamp.ToUniversalTime()).ToUnixTimeSeconds(),
            Assert.IsType<AmqpTimestamp>(headers["local"]).UnixTime);
        Assert.Equal(identifier.ToString("D"), headers["guid"]);
        Assert.Equal(new[] { "copy@example.test" }, Assert.IsType<string[]>(headers["CC"]));
        Assert.Equal(new[] { "first@example.test", "second@example.test" }, Assert.IsType<string[]>(headers["BCC"]));
        Assert.Equal("https://example.test/resource", headers["uri"]);
        Assert.Equal("value", headers["text"]);
        Assert.Equal(bool.TrueString, headers["true"]);
        Assert.Equal(bool.FalseString, headers["false"]);
        Assert.Equal(12.5m, headers["decimal"]);
        Assert.Equal("12.5", headers["formattable"]);
        Assert.DoesNotContain("unsupported", headers.Keys);
        Assert.DoesNotContain(RabbitMqHeaders.Exchange, headers.Keys);
        Assert.DoesNotContain(RabbitMqHeaders.RoutingKey, headers.Keys);
        Assert.DoesNotContain(RabbitMqHeaders.DeliveryTag, headers.Keys);
        Assert.DoesNotContain(RabbitMqHeaders.ConsumerTag, headers.Keys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "complete-publish-frame")]
    public async Task SendAsync_PublishesTheExactEnvelopeAndAmqpPropertiesAsync()
    {
        byte[] body = { 1, 2, 3, 4 };
        Guid messageId = Guid.Parse("b70f751c-58d0-4ac8-8549-b00d3aa91a32");
        Guid correlationId = Guid.Parse("7274f764-414d-43cd-aae8-e9bbfdd5818e");
        var context = CreateMessageContext("orders", body);
        context.MessageId = messageId;
        context.CorrelationId = correlationId;
        context.RequestId = Guid.NewGuid();
        context.ResponseAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
        context.RoutingKey = "orders.created";
        context.Mandatory = true;
        context.AwaitAck = false;
        context.TimeToLive = TimeSpan.FromMilliseconds(1250);
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        PublishedFrame published = Assert.Single(channel.Published);
        Assert.Equal("orders", published.Exchange);
        Assert.Equal("orders.created", published.RoutingKey);
        Assert.True(published.Mandatory);
        Assert.False(published.AwaitAck);
        Assert.Equal(body, published.Body);
        Assert.Equal("application/vnd.vicione.test", published.BasicProperties.ContentType);
        Assert.True(published.BasicProperties.Persistent);
        Assert.Equal(messageId.ToString(), published.BasicProperties.MessageId);
        Assert.Equal(correlationId.ToString(), published.BasicProperties.CorrelationId);
        Assert.Equal("1250", published.BasicProperties.Expiration);
        Assert.Equal(RabbitMqExchangeNames.ReplyTo, published.BasicProperties.ReplyTo);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "serializer-cannot-change-published-frame-metadata")]
    public async Task SendAsync_RejectsBodyCallbackThatChangesPublishedMetadataAsync(bool changeId, bool changeContentType)
    {
        Guid initialId = Guid.Parse("b70f751c-58d0-4ac8-8549-b00d3aa91a32");
        Guid laterId = Guid.Parse("7274f764-414d-43cd-aae8-e9bbfdd5818e");
        var context = CreateMessageContext("orders", [1, 2, 3, 4]);
        context.MessageId = initialId;
        context.Serializer = new MutatingSerializer(sendContext =>
        {
            if (changeId)
                sendContext.MessageId = laterId;
            if (changeContentType)
                sendContext.ContentType = new ContentType("application/vnd.vicione.changed");
        });
        var channel = new RecordingChannelContext();

        MessageException failure = await Assert.ThrowsAsync<MessageException>(() =>
            CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Contains(changeId ? "MessageId changed" : "ContentType changed", failure.Message, StringComparison.Ordinal);
        Assert.Equal(initialId, context.MessageId);
        Assert.Equal("application/vnd.vicione.test", context.ContentType?.ToString());
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "serializer-cannot-change-published-correlation")]
    public async Task SendAsync_RejectsBodyCallbackThatChangesPublishedCorrelationAsync()
    {
        Guid initialCorrelationId = Guid.Parse("7274f764-414d-43cd-aae8-e9bbfdd5818e");
        Guid laterCorrelationId = Guid.Parse("b70f751c-58d0-4ac8-8549-b00d3aa91a32");
        var context = CreateMessageContext("orders", [1, 2, 3, 4]);
        context.CorrelationId = initialCorrelationId;
        context.Serializer = new MutatingSerializer(sendContext => sendContext.CorrelationId = laterCorrelationId);
        var channel = new RecordingChannelContext();

        MessageException failure = await Assert.ThrowsAsync<MessageException>(() =>
            CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Contains("CorrelationId changed during serialization", failure.Message, StringComparison.Ordinal);
        Assert.Equal(initialCorrelationId, context.CorrelationId);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "distinct-publish-payload-enforces-mandatory-routing")]
    public async Task SendAsync_EnforcesMandatoryRoutingFromADistinctPublishPayloadAsync()
    {
        PublishContext publish = DispatchProxy.Create<PublishContext, MandatoryPublishPayloadProxy>();
        var context = new PublishPayloadMessageContext(publish);
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.True(context.Mandatory);
        Assert.True(Assert.Single(channel.Published).Mandatory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "nonpositive-ttl-minimum")]
    public async Task SendAsync_ClampsNonpositiveTimeToLiveToOneSecondAsync(int milliseconds)
    {
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.TimeToLive = TimeSpan.FromMilliseconds(milliseconds);
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.Equal("1000", Assert.Single(channel.Published).BasicProperties.Expiration);
    }

    [Theory]
    [InlineData(1L, "1")]
    [InlineData(5_000L, "1")]
    [InlineData(11_000L, "2")]
    [InlineData(12_500_000L, "1250")]
    [InlineData(9_223_372_036_854_770_001L, "922337203685478")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "positive-ttl-never-rounds-to-zero-or-shorter")]
    public async Task SendAsync_RoundsPositiveTimeToLiveUpToTheNextWireMillisecondAsync(long ticks, string expectedExpiration)
    {
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.TimeToLive = TimeSpan.FromTicks(ticks);
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.Equal(expectedExpiration, Assert.Single(channel.Published).BasicProperties.Expiration);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "direct-reply-to-uses-default-exchange")]
    public async Task SendAsync_DirectReplyToUsesTheDefaultExchangeAndPreservesTheReplyRoutingKeyAsync()
    {
        var context = CreateMessageContext(RabbitMqExchangeNames.ReplyTo, new byte[] { 1, 2 });
        context.RoutingKey = "amq.rabbitmq.reply-to.reply-42";
        var delayPipe = new CountingPipe();
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology(), delayPipe, RabbitMqExchangeNames.ReplyTo)
            .SendAsync(channel, context, TestContext.Current.CancellationToken);

        PublishedFrame frame = Assert.Single(channel.Published);
        Assert.Equal(string.Empty, frame.Exchange);
        Assert.Equal("amq.rabbitmq.reply-to.reply-42", frame.RoutingKey);
        Assert.Equal(new byte[] { 1, 2 }, frame.Body);
        Assert.Equal(0, delayPipe.Calls);
        Assert.DoesNotContain("x-delay", frame.BasicProperties.Headers!.Keys);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("orders.created", true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "routing-key-telemetry-only-for-a-present-route")]
    public async Task SendAsync_RecordsRoutingKeyOnlyWhenTheActivityHasAConcreteRouteAsync(string? routingKey, bool expectTag)
    {
        using var activity = new Activity("rabbitmq-send") { IsAllDataRequested = true };
        activity.Start();
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.RoutingKey = routingKey;
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology()).SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.Single(channel.Published);
        Assert.Equal(expectTag ? routingKey : null,
            activity.GetTagItem(ServiceBusTelemetry.Attributes.RabbitMqRoutingKey));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "delayed-route-and-header")]
    public async Task SendAsync_DeclaresDelayTopologyAndPublishesThroughTheDelayExchangeAsync()
    {
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.RoutingKey = "orders.created";
        context.Delay = TimeSpan.FromMilliseconds(1750);
        var delayPipe = new CountingPipe();
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology(), delayPipe).SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.Equal(1, delayPipe.Calls);
        PublishedFrame published = Assert.Single(channel.Published);
        Assert.Equal("orders_delay", published.Exchange);
        Assert.Equal("orders.created", published.RoutingKey);
        Assert.Equal(1750L, published.BasicProperties.Headers!["x-delay"]);
    }

    [Theory]
    [InlineData(1L, 1L)]
    [InlineData(5_000L, 1L)]
    [InlineData(11_000L, 2L)]
    [InlineData(9_223_372_036_854_770_001L, 922_337_203_685_478L)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "positive-delay-never-becomes-zero-on-the-wire")]
    public async Task SendAsync_RoundsPositiveDelayUpToTheNextWireMillisecondAsync(long ticks, long expectedMilliseconds)
    {
        var context = CreateMessageContext("orders", new byte[] { 1 });
        context.Delay = TimeSpan.FromTicks(ticks);
        var delayPipe = new CountingPipe();
        var channel = new RecordingChannelContext();

        await CreateTransport(EmptyTopology(), delayPipe).SendAsync(channel, context, TestContext.Current.CancellationToken);

        PublishedFrame frame = Assert.Single(channel.Published);
        Assert.Equal("orders_delay", frame.Exchange);
        Assert.Equal(expectedMilliseconds, frame.BasicProperties.Headers!["x-delay"]);
        Assert.Equal(1, delayPipe.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "publish-failure-invalidates-topology")]
    public async Task SendAsync_PublishFailureEvictsOneTimeSetupAndInvalidatesTheConnectionCacheAsync(bool synchronous)
    {
        var failure = new InvalidOperationException("publish failed");
        var channel = new RecordingChannelContext { ThrowPublishFailuresSynchronously = synchronous };
        channel.PublishOutcomes.Enqueue(failure);
        channel.PublishOutcomes.Enqueue(null);
        BrokerTopology topology = ExchangeTopology("orders");
        RabbitMqSendTransportContext transport = CreateTransport(topology);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.SendAsync(channel, CreateMessageContext("orders", new byte[] { 1 }), TestContext.Current.CancellationToken));
        await transport.SendAsync(channel, CreateMessageContext("orders", new byte[] { 2 }), TestContext.Current.CancellationToken);

        Assert.Same(failure, actual);
        Assert.Equal(2, channel.ExchangeDeclarations);
        Assert.Equal(2, channel.Published.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "caller-cancellation-before-broker-work")]
    public async Task SendAsync_CallerCancellationStopsBeforeTopologyOrPublishAsync()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var channel = new RecordingChannelContext();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, CreateMessageContext("orders", new byte[] { 1 }), source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(0, channel.ExchangeDeclarations);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "publisher-confirmation-required")]
    public async Task DurableAcceptance_RejectsDisabledPublisherConfirmationBeforeBrokerWorkAsync()
    {
        var context = CreateAcceptedMessageContext();
        context.GetOrAddPayload(() => new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: false));
        var channel = new RecordingChannelContext(publisherConfirmation: false);

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Contains("publisher confirmations are disabled", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, channel.ExchangeDeclarations);
        Assert.Empty(channel.Published);
    }

    [Theory]
    [InlineData(InvalidAcceptance.Exchange)]
    [InlineData(InvalidAcceptance.Delay)]
    [InlineData(InvalidAcceptance.TimeToLive)]
    [InlineData(InvalidAcceptance.NonDurable)]
    [InlineData(InvalidAcceptance.NonMandatory)]
    [InlineData(InvalidAcceptance.NoAcknowledgement)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "route-and-delivery-invariants")]
    public async Task DurableAcceptance_RejectsEveryRouteOrDeliveryOverrideAsync(InvalidAcceptance invalid)
    {
        RabbitMqMessageSendContext<TestMessage> context = invalid == InvalidAcceptance.Exchange
            ? CreateMessageContext("other", new byte[] { 1 })
            : CreateAcceptedMessageContext();
        ApplyInvalidAcceptance(context, invalid);
        context.GetOrAddPayload(() => new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: false));
        var channel = new RecordingChannelContext();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Contains("publish route or delivery properties differ", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, channel.ExchangeDeclarations);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "transport-exchange-must-match-validated-destination")]
    public async Task DurableAcceptance_RejectsATransportExchangeThatDiffersFromTheValidatedDestinationAsync()
    {
        var context = CreateAcceptedMessageContext();
        var requirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: false);
        context.GetOrAddPayload(() => requirement);
        var channel = new RecordingChannelContext();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CreateTransport(ExchangeTopology("orders"), exchange: "unvalidated")
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Contains("publish route or delivery properties differ", exception.Message, StringComparison.Ordinal);
        Assert.False(requirement.Accepted);
        Assert.Equal(0, channel.ExchangeDeclarations);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "existing-quorum-queue-proof-before-acceptance")]
    public async Task DurableAcceptance_ProvesTheExistingQuorumQueueBeforeMarkingTheSendAcceptedAsync()
    {
        var context = CreateAcceptedMessageContext();
        var requirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        context.GetOrAddPayload(() => requirement);
        var channel = new RecordingChannelContext();

        await CreateTransport(ExchangeTopology("orders"))
            .SendAsync(channel, context, TestContext.Current.CancellationToken);

        Assert.True(requirement.Accepted);
        Assert.Equal(new[] { "passive:orders", "declare:orders", "passive:orders", "declare:orders" }, channel.QueueProofOperations);
        Assert.Equal("quorum", channel.QueueDeclarationArguments![RabbitMQ.Client.Headers.XQueueType]);
        Assert.Single(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "queue-replacement-cannot-be-accepted")]
    public async Task DurableAcceptance_RejectsAQueueRecreatedBetweenExistenceProofAndPublishAsync()
    {
        var context = CreateAcceptedMessageContext();
        var requirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        context.GetOrAddPayload(() => requirement);
        var channel = new RecordingChannelContext { RecreateQueueWhenQuorumDeclared = true };

        MessageReturnedException exception = await Assert.ThrowsAsync<MessageReturnedException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Equal("replacement queue is not bound", exception.Message);
        Assert.False(requirement.Accepted);
        Assert.Equal(new[] { "passive:orders", "declare:orders" }, channel.QueueProofOperations);
        Assert.Single(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "routable-classic-replacement-is-rejected-after-confirmation")]
    public async Task DurableAcceptance_RejectsARoutableClassicReplacementAfterPublishConfirmationAsync()
    {
        var context = CreateAcceptedMessageContext();
        var requirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        context.GetOrAddPayload(() => requirement);
        var failure = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "replacement is classic"));
        var channel = new RecordingChannelContext { PostPublishQueueProofFailure = failure };

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Same(failure, exception.InnerException);
        Assert.False(requirement.Accepted);
        Assert.Equal(new[] { "passive:orders", "declare:orders", "passive:orders", "declare:orders" }, channel.QueueProofOperations);
        Assert.Single(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "concurrent-failures-cannot-mask-broker-cause")]
    public async Task DurableAcceptance_ConcurrentFailuresPreserveTheirBrokerCausesDuringTopologyRebuildAsync()
    {
        var firstContext = CreateAcceptedMessageContext();
        var firstRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        firstContext.GetOrAddPayload(() => firstRequirement);
        var secondContext = CreateAcceptedMessageContext();
        var secondRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        secondContext.GetOrAddPayload(() => secondRequirement);
        var recoveryContext = CreateAcceptedMessageContext();
        var recoveryRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        recoveryContext.GetOrAddPayload(() => recoveryRequirement);
        var firstFailure = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "first replacement is classic"));
        var secondFailure = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "second replacement is classic"));
        var firstProofEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstProof = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondProofEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecondProof = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var topologyRebuildEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTopologyRebuild = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = new RecordingChannelContext
        {
            PostPublishQueueProofFailure = firstFailure,
            PostPublishQueueProofEntered = firstProofEntered,
            ReleasePostPublishQueueProof = releaseFirstProof,
            SecondPostPublishQueueProofFailure = secondFailure,
            SecondPostPublishQueueProofEntered = secondProofEntered,
            ReleaseSecondPostPublishQueueProof = releaseSecondProof,
            TopologyRebuildEntered = topologyRebuildEntered,
            ReleaseTopologyRebuild = releaseTopologyRebuild,
        };
        RabbitMqSendTransportContext transport = CreateTransport(ExchangeTopology("orders"));

        try
        {
            Task firstSend = transport.SendAsync(channel, firstContext, TestContext.Current.CancellationToken);
            await firstProofEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Task secondSend = transport.SendAsync(channel, secondContext, TestContext.Current.CancellationToken);
            await secondProofEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

            releaseFirstProof.SetResult();
            ConfigurationException firstException = await Assert.ThrowsAsync<ConfigurationException>(() => firstSend);

            Task recoverySend = transport.SendAsync(channel, recoveryContext, TestContext.Current.CancellationToken);
            await topologyRebuildEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            releaseSecondProof.SetResult();
            ConfigurationException secondException = await Assert.ThrowsAsync<ConfigurationException>(() => secondSend);
            releaseTopologyRebuild.SetResult();
            await recoverySend;

            Assert.Same(firstFailure, firstException.InnerException);
            Assert.Same(secondFailure, secondException.InnerException);
            Assert.False(firstRequirement.Accepted);
            Assert.False(secondRequirement.Accepted);
            Assert.True(recoveryRequirement.Accepted);
            Assert.Equal(3, channel.Published.Count);
            Assert.Equal(3, channel.ExchangeDeclarations);
        }
        finally
        {
            releaseFirstProof.TrySetResult();
            releaseSecondProof.TrySetResult();
            releaseTopologyRebuild.TrySetResult();
        }
    }

    [Theory]
    [InlineData(399, false)]
    [InlineData(400, true)]
    [InlineData(499, true)]
    [InlineData(500, false)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "only-4xx-proof-failures-are-terminal-configuration-errors")]
    public async Task DurableAcceptance_ClassifiesOnlyAmqp4xxProofFailuresAsConfigurationErrorsAsync(int replyCode, bool terminal)
    {
        var context = CreateAcceptedMessageContext();
        context.GetOrAddPayload(() => new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true));
        var failure = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, (ushort)replyCode, $"reply-{replyCode}"));
        var channel = new RecordingChannelContext { QueueProofFailure = failure };

        Exception actual = Assert.IsAssignableFrom<Exception>(await Record.ExceptionAsync(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken)));

        if (terminal)
        {
            ConfigurationException configuration = Assert.IsType<ConfigurationException>(actual);
            Assert.Same(failure, configuration.InnerException);
            Assert.Contains(replyCode.ToString(CultureInfo.InvariantCulture), configuration.Message, StringComparison.Ordinal);
        }
        else
            Assert.Same(failure, actual);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "non-amqp-proof-failure-is-preserved")]
    public async Task DurableAcceptance_PreservesNonAmqpProofFailuresAsync()
    {
        var context = CreateAcceptedMessageContext();
        context.GetOrAddPayload(() => new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true));
        var failure = new IOException("transport unavailable");
        var channel = new RecordingChannelContext { QueueProofFailure = failure };

        IOException actual = await Assert.ThrowsAsync<IOException>(
            () => CreateTransport(ExchangeTopology("orders"))
                .SendAsync(channel, context, TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Empty(channel.Published);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "canceled-prepublish-proof-preserves-topology")]
    public async Task DurableAcceptance_CanceledQueueProofDoesNotPublishOrEvictValidTopologyAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var canceledContext = CreateAcceptedMessageContext(cancellation);
        var canceledRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        canceledContext.GetOrAddPayload(() => canceledRequirement);
        var retryContext = CreateAcceptedMessageContext();
        var retryRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        retryContext.GetOrAddPayload(() => retryRequirement);
        var channel = new RecordingChannelContext { CancelDuringQueueProof = cancellation };
        RabbitMqSendTransportContext transport = CreateTransport(ExchangeTopology("orders"));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendAsync(channel, canceledContext, TestContext.Current.CancellationToken));
        await transport.SendAsync(channel, retryContext, TestContext.Current.CancellationToken);

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.False(canceledRequirement.Accepted);
        Assert.True(retryRequirement.Accepted);
        Assert.Equal(1, channel.ExchangeDeclarations);
        Assert.Single(channel.Published);
        Assert.Equal(new[] { "passive:orders", "passive:orders", "declare:orders", "passive:orders", "declare:orders" },
            channel.QueueProofOperations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-ACCEPTANCE", "canceled-publish-preserves-topology")]
    public async Task DurableAcceptance_CanceledPublishDoesNotMarkAcceptanceOrEvictValidTopologyAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var canceledContext = CreateAcceptedMessageContext(cancellation);
        var canceledRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        canceledContext.GetOrAddPayload(() => canceledRequirement);
        var retryContext = CreateAcceptedMessageContext();
        var retryRequirement = new RabbitMqTransportAcceptanceRequirement("orders", requiresExistingQueueProof: true);
        retryContext.GetOrAddPayload(() => retryRequirement);
        var channel = new RecordingChannelContext { CancelDuringPublish = cancellation };
        RabbitMqSendTransportContext transport = CreateTransport(ExchangeTopology("orders"));

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendAsync(channel, canceledContext, TestContext.Current.CancellationToken));
        await transport.SendAsync(channel, retryContext, TestContext.Current.CancellationToken);

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.False(canceledRequirement.Accepted);
        Assert.True(retryRequirement.Accepted);
        Assert.Equal(1, channel.ExchangeDeclarations);
        Assert.Equal(2, channel.Published.Count);
        Assert.Equal(new[] { "passive:orders", "declare:orders", "passive:orders", "declare:orders",
            "passive:orders", "declare:orders" }, channel.QueueProofOperations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "incoming-reply-properties-copy-with-explicit-precedence")]
    public async Task CreateSendContextAsync_CopiesIncomingReplyPropertiesWithoutOverwritingExplicitValuesAsync()
    {
        ConsumeContext incoming = CreateIncomingContext(new BasicProperties
        {
            Priority = 7,
            ReplyTo = "incoming.reply",
        });
        var copyPipe = new ConfigureSendPipe(context =>
        {
            context.ResponseAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
            context.GetOrAddPayload(() => incoming);
        });
        var explicitPipe = new ConfigureSendPipe(context =>
        {
            context.ResponseAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
            context.BasicProperties.Priority = 9;
            context.BasicProperties.ReplyTo = "explicit.reply";
            context.GetOrAddPayload(() => incoming);
        });
        RabbitMqSendTransportContext transport = CreateTransport(EmptyTopology());

        var copied = Assert.IsType<RabbitMqMessageSendContext<TestMessage>>(
            await transport.CreateSendContextAsync(new TestMessage(), copyPipe, TestContext.Current.CancellationToken));
        var explicitValues = Assert.IsType<RabbitMqMessageSendContext<TestMessage>>(
            await transport.CreateSendContextAsync(new TestMessage(), explicitPipe, TestContext.Current.CancellationToken));

        Assert.Equal<byte>(7, copied.BasicProperties.Priority);
        Assert.Equal("incoming.reply", copied.BasicProperties.ReplyTo);
        Assert.Equal<byte>(9, explicitValues.BasicProperties.Priority);
        Assert.Equal("explicit.reply", explicitValues.BasicProperties.ReplyTo);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "reply-to-requires-routing-key")]
    public async Task CreateSendContextAsync_RejectsReplyToWithoutARoutingKeyAsync()
    {
        var pipe = new ConfigureSendPipe(context =>
            context.DestinationAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to"));

        TransportException exception = await Assert.ThrowsAsync<TransportException>(
            () => CreateTransport(EmptyTopology(), exchange: RabbitMqExchangeNames.ReplyTo)
                .CreateSendContextAsync(new TestMessage(), pipe, TestContext.Current.CancellationToken));

        Assert.Equal(new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to"), exception.Uri);
        Assert.Contains("RoutingKey must be specified", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "channel-bound-reply-preserves-wire-properties")]
    public async Task CreateSendContextAsync_ChannelBoundReplyPreservesIncomingPropertiesThroughPublishAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var message = new TestMessage();
        ConsumeContext incoming = CreateIncomingContext(new BasicProperties
        {
            Priority = 7,
            ReplyTo = "incoming.reply",
        });
        var pipe = new ConfigureSendPipe(context =>
        {
            context.DestinationAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
            context.ResponseAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
            context.RoutingKey = "reply.queue";
            context.Serializer = new BinarySerializer(new byte[] { 1, 2 });
            context.GetOrAddPayload(() => incoming);
        });
        var channel = new RecordingChannelContext();
        RabbitMqSendTransportContext transport = CreateTransport(EmptyTopology(), exchange: RabbitMqExchangeNames.ReplyTo);

        var sendContext = Assert.IsType<RabbitMqMessageSendContext<TestMessage>>(
            await transport.CreateSendContextAsync(channel, message, pipe, cancellation.Token));
        await transport.SendAsync(channel, sendContext, cancellation.Token);

        Assert.Same(message, sendContext.Message);
        Assert.Equal(cancellation.Token, sendContext.CancellationToken);
        PublishedFrame published = Assert.Single(channel.Published);
        Assert.Equal("", published.Exchange);
        Assert.Equal("reply.queue", published.RoutingKey);
        Assert.Equal(new byte[] { 1, 2 }, published.Body);
        Assert.Equal<byte>(7, published.BasicProperties.Priority);
        Assert.Equal("incoming.reply", published.BasicProperties.ReplyTo);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-CONTEXT", "channel-bound-reply-rejects-missing-route")]
    public async Task CreateSendContextAsync_ChannelBoundReplyRejectsMissingRouteBeforePublishAsync()
    {
        var channel = new RecordingChannelContext();
        var pipe = new ConfigureSendPipe(context =>
        {
            context.DestinationAddress = new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to");
            context.RoutingKey = "   ";
        });
        RabbitMqSendTransportContext transport = CreateTransport(EmptyTopology(), exchange: RabbitMqExchangeNames.ReplyTo);

        TransportException exception = await Assert.ThrowsAsync<TransportException>(() =>
            transport.CreateSendContextAsync(channel, new TestMessage(), pipe, TestContext.Current.CancellationToken));

        Assert.Equal(new Uri("rabbitmq://localhost/amq.rabbitmq.reply-to"), exception.Uri);
        Assert.Contains("RoutingKey must be specified", exception.Message, StringComparison.Ordinal);
        Assert.Empty(channel.Published);
    }

    private static void ApplyInvalidAcceptance(RabbitMqMessageSendContext<TestMessage> context, InvalidAcceptance invalid)
    {
        switch (invalid)
        {
            case InvalidAcceptance.Delay:
                context.Delay = TimeSpan.FromSeconds(1);
                break;
            case InvalidAcceptance.TimeToLive:
                context.TimeToLive = TimeSpan.FromSeconds(1);
                break;
            case InvalidAcceptance.NonDurable:
                context.Durable = false;
                break;
            case InvalidAcceptance.NonMandatory:
                context.Mandatory = false;
                break;
            case InvalidAcceptance.NoAcknowledgement:
                context.AwaitAck = false;
                break;
        }
    }

    private static RabbitMqMessageSendContext<TestMessage> CreateAcceptedMessageContext()
    {
        RabbitMqMessageSendContext<TestMessage> context = CreateMessageContext("orders", new byte[] { 1 });
        context.Mandatory = true;
        return context;
    }

    private static RabbitMqMessageSendContext<TestMessage> CreateAcceptedMessageContext(CancellationTokenSource cancellation)
    {
        var context = new RabbitMqMessageSendContext<TestMessage>(
            new BasicProperties(), "orders", new TestMessage(), cancellation.Token)
        {
            Serializer = new BinarySerializer(new byte[] { 1 }),
            Mandatory = true,
        };
        return context;
    }

    private static RabbitMqMessageSendContext<TestMessage> CreateMessageContext(string exchange, byte[] body)
    {
        var context = new RabbitMqMessageSendContext<TestMessage>(new BasicProperties(), exchange, new TestMessage(), CancellationToken.None)
        {
            Serializer = new BinarySerializer(body),
        };
        return context;
    }

    private static RabbitMqSendTransportContext CreateTransport(BrokerTopology topology, IPipe<ChannelContext>? delayPipe = null,
        string exchange = "orders")
    {
        ISerialization serialization = DispatchProxy.Create<ISerialization, PassiveProxy>();
        ReceiveEndpointContext endpoint = DispatchProxy.Create<ReceiveEndpointContext, ReceiveEndpointProxy>();
        ((ReceiveEndpointProxy)(object)endpoint).Serialization = serialization;
        IRabbitMqHostConfiguration host = DispatchProxy.Create<IRabbitMqHostConfiguration, PassiveProxy>();
        IChannelContextSupervisor supervisor = DispatchProxy.Create<IChannelContextSupervisor, PassiveProxy>();
        var settings = new TestSendSettings(topology);
        var topologyFilter = new ConfigureRabbitMqTopologyFilter<SendSettings>(settings, topology);

        return new RabbitMqSendTransportContext(
            host,
            endpoint,
            supervisor,
            topologyFilter,
            exchange,
            delayPipe ?? Pipe.Empty<ChannelContext>(),
            "orders_delay");
    }

    private static BrokerTopology EmptyTopology() => new RabbitMqBrokerTopology([], [], [], []);

    private static BrokerTopology ExchangeTopology(string name)
    {
        var exchange = new ExchangeEntity(1, name, RabbitMQ.Client.ExchangeType.Fanout, true, false,
            new Dictionary<string, object?>());
        return new RabbitMqBrokerTopology(new[] { exchange }, [], [], []);
    }

    public enum InvalidAcceptance
    {
        Exchange,
        Delay,
        TimeToLive,
        NonDurable,
        NonMandatory,
        NoAcknowledgement,
    }

    private sealed class TestMessage;

    private sealed class PublishPayloadMessageContext : RabbitMqMessageSendContext<TestMessage>
    {
        private readonly PublishContext _publish;

        public PublishPayloadMessageContext(PublishContext publish)
            : base(new BasicProperties(), "orders", new TestMessage(), CancellationToken.None)
        {
            _publish = publish;
            Serializer = new BinarySerializer(new byte[] { 1 });
        }

        public override bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
            where TPayload : class
        {
            if (typeof(TPayload) == typeof(PublishContext))
            {
                payload = (TPayload)(object)_publish;
                return true;
            }

            return base.TryGetPayload(out payload);
        }
    }

    private sealed class BinarySerializer(byte[] body) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/vnd.vicione.test");

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class => new BinaryMessageBody(body);
    }

    private sealed class MutatingSerializer(Action<SendContext> mutate) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/vnd.vicione.test");

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new MutatingBody(() => mutate(context));
    }

    private sealed class MutatingBody(Action mutate) : MessageBody
    {
        public long Length => 4;

        public byte[] ToArray()
        {
            mutate();
            return [1, 2, 3, 4];
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            text = Convert.ToBase64String(ToArray());
            return true;
        }
    }

    private sealed class InvariantFormattable(decimal value) : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => value.ToString(format, formatProvider);
    }

    private sealed class CountingPipe : IPipe<ChannelContext>
    {
        public int Calls { get; private set; }

        public Task SendAsync(ChannelContext context)
        {
            Calls++;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class ConfigureSendPipe(Action<RabbitMqMessageSendContext<TestMessage>> configure) : IPipe<SendContext<TestMessage>>
    {
        public Task SendAsync(SendContext<TestMessage> context)
        {
            configure(Assert.IsType<RabbitMqMessageSendContext<TestMessage>>(context));
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private static ConsumeContext CreateIncomingContext(IReadOnlyBasicProperties properties)
    {
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).BasicContext = new IncomingBasicContext(properties);
        return context;
    }

    private sealed record IncomingBasicContext(IReadOnlyBasicProperties Properties) : RabbitMqBasicConsumeContext
    {
        public string Exchange => "incoming";
        public string RoutingKey => "incoming.route";
        public string ConsumerTag => "consumer";
        public ulong DeliveryTag => 1;
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        public RabbitMqBasicConsumeContext BasicContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                if (targetMethod.GetGenericArguments()[0] == typeof(RabbitMqBasicConsumeContext))
                {
                    args![0] = BasicContext;
                    return true;
                }

                args![0] = null;
                return false;
            }

            return PassiveProxy.Default(targetMethod);
        }
    }

    private sealed class TestSendSettings(BrokerTopology topology) : SendSettings
    {
        public bool Durable => true;
        public bool AutoDelete => false;
        public IDictionary<string, object?> ExchangeArguments { get; } = new Dictionary<string, object?>();
        public string ExchangeName => "orders";
        public string ExchangeType => RabbitMQ.Client.ExchangeType.Fanout;
        public RabbitMqEndpointAddress GetSendAddress(Uri hostAddress) => throw new NotSupportedException();
        public BrokerTopology GetBrokerTopology() => topology;
    }

    private sealed class RecordingChannelContext : BasePipeContext, ChannelContext
    {
        private int _exchangeDeclarations;
        private bool _queueRoutable = true;
        private int _queueDeclarations;

        public RecordingChannelContext(bool publisherConfirmation = true)
        {
            ConnectionContext = new TestConnectionContext(publisherConfirmation);
        }

        public System.Collections.Generic.Queue<Exception?> PublishOutcomes { get; } = new();
        public List<PublishedFrame> Published { get; } = [];
        public List<string> QueueProofOperations { get; } = [];
        public IDictionary<string, object?>? QueueDeclarationArguments { get; private set; }
        public Exception? PostPublishQueueProofFailure { get; init; }
        public TaskCompletionSource? PostPublishQueueProofEntered { get; init; }
        public Exception? QueueProofFailure { get; init; }
        public CancellationTokenSource? CancelDuringQueueProof { get; set; }
        public CancellationTokenSource? CancelDuringPublish { get; set; }
        public TaskCompletionSource? ReleasePostPublishQueueProof { get; init; }
        public TaskCompletionSource? ReleaseSecondPostPublishQueueProof { get; init; }
        public TaskCompletionSource? ReleaseTopologyRebuild { get; init; }
        public bool RecreateQueueWhenQuorumDeclared { get; init; }
        public Exception? SecondPostPublishQueueProofFailure { get; init; }
        public TaskCompletionSource? SecondPostPublishQueueProofEntered { get; init; }
        public bool ThrowPublishFailuresSynchronously { get; init; }
        public TaskCompletionSource? TopologyRebuildEntered { get; init; }
        public int ExchangeDeclarations => Volatile.Read(ref _exchangeDeclarations);
        public IChannel Channel => null!;
        public ConnectionContext ConnectionContext { get; }

        public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body,
            bool awaitAck, CancellationToken cancellationToken)
        {
            Published.Add(new PublishedFrame(exchange, routingKey, mandatory, basicProperties, body, awaitAck));
            if (CancelDuringPublish is { } source)
            {
                CancelDuringPublish = null;
                source.Cancel();
                return Task.FromCanceled(cancellationToken);
            }
            if (RecreateQueueWhenQuorumDeclared && !_queueRoutable)
                return Task.FromException(new MessageReturnedException("replacement queue is not bound"));
            Exception? outcome = PublishOutcomes.Count > 0 ? PublishOutcomes.Dequeue() : null;
            if (outcome != null && ThrowPublishFailuresSynchronously)
                throw outcome;
            return outcome == null ? Task.CompletedTask : Task.FromException(outcome);
        }

        public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete,
            IDictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            int declaration = Interlocked.Increment(ref _exchangeDeclarations);
            if (declaration == 2 && TopologyRebuildEntered is not null && ReleaseTopologyRebuild is not null)
            {
                TopologyRebuildEntered.TrySetResult();
                await ReleaseTopologyRebuild.Task.WaitAsync(cancellationToken);
            }
        }

        public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
        {
            QueueProofOperations.Add($"passive:{queue}");
            if (CancelDuringQueueProof is { } source)
            {
                CancelDuringQueueProof = null;
                source.Cancel();
                return Task.FromCanceled<QueueDeclareOk>(cancellationToken);
            }
            if (QueueProofFailure != null)
                return Task.FromException<QueueDeclareOk>(QueueProofFailure);
            return Task.FromResult<QueueDeclareOk>(null!);
        }

        public Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete,
            IDictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            QueueProofOperations.Add($"declare:{queue}");
            QueueDeclarationArguments = new Dictionary<string, object?>(arguments);
            int declaration = Interlocked.Increment(ref _queueDeclarations);
            if (declaration == 2 && PostPublishQueueProofFailure != null)
                return FailQueueProofAsync(PostPublishQueueProofFailure, PostPublishQueueProofEntered,
                    ReleasePostPublishQueueProof, cancellationToken);
            if (declaration == 4 && SecondPostPublishQueueProofFailure != null)
                return FailQueueProofAsync(SecondPostPublishQueueProofFailure, SecondPostPublishQueueProofEntered,
                    ReleaseSecondPostPublishQueueProof, cancellationToken);
            if (RecreateQueueWhenQuorumDeclared)
                _queueRoutable = false;
            return Task.FromResult<QueueDeclareOk>(null!);
        }

        static async Task<QueueDeclareOk> FailQueueProofAsync(Exception failure, TaskCompletionSource? entered,
            TaskCompletionSource? release, CancellationToken cancellationToken)
        {
            entered?.TrySetResult();
            if (release is not null)
                await release.Task.WaitAsync(cancellationToken);
            throw failure;
        }

        public Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments,
            CancellationToken cancellationToken)
        {
            QueueProofOperations.Add($"bind:{queue}:{exchange}:{routingKey}");
            _queueRoutable = true;
            return Task.CompletedTask;
        }

        public Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments,
            IAsyncBasicConsumer consumer, string consumerTag, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void NotifyFaulted(Exception exception, Uri inputAddress) => throw new NotSupportedException();
    }

    private sealed class TestConnectionContext(bool publisherConfirmation) : BasePipeContext, ConnectionContext
    {
        public IConnection Connection => null!;
        public string Description => "test";
        public Uri HostAddress { get; } = new("rabbitmq://localhost/");
        public bool PublisherConfirmation { get; } = publisherConfirmation;
        public BatchSettings BatchSettings => null!;
        public TimeSpan ContinuationTimeout => TimeSpan.FromSeconds(1);
        public TimeSpan StopTimeout => TimeSpan.FromSeconds(1);
        public IRabbitMqBusTopology Topology => null!;
        public RabbitMqTopologyEntityCache TopologyEntityCache { get; } = new();
        public Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private record PublishedFrame(
        string Exchange,
        string RoutingKey,
        bool Mandatory,
        BasicProperties BasicProperties,
        byte[] Body,
        bool AwaitAck);

    private class ReceiveEndpointProxy : DispatchProxy
    {
        public ISerialization Serialization { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Serialization" ? Serialization : PassiveProxy.Default(targetMethod);
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Default(targetMethod);

        public static object? Default(MethodInfo? method)
        {
            if (method?.Name == nameof(PipeContext.TryGetPayload) && method.IsGenericMethod)
            {
                return false;
            }
            if (method?.ReturnType == typeof(void))
                return null;
            return method?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(method.ReturnType)
                : null;
        }
    }

    private class MandatoryPublishPayloadProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Mandatory" ? true : PassiveProxy.Default(targetMethod);
    }
}
