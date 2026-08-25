using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class EnvelopeMetadataProjectionTests
{
    private static readonly DateTimeOffset ProjectionTime =
        new(2026, 8, 24, 9, 10, 11, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "send-context-all-fields-and-clock")]
    public void SendContext_ProjectsEveryMetadataFieldAndUsesTheInjectedClock()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = CreateSendContext();
        context.TimeToLive = TimeSpan.FromMinutes(7);
        context.SetTimeProvider(timeProvider);

        var envelope = new JsonMessageEnvelope(context, context.Message);

        Assert.Equal(context.MessageId?.ToString(), envelope.MessageId);
        Assert.Equal(context.RequestId?.ToString(), envelope.RequestId);
        Assert.Equal(context.CorrelationId?.ToString(), envelope.CorrelationId);
        Assert.Equal(context.ConversationId?.ToString(), envelope.ConversationId);
        Assert.Equal(context.InitiatorId?.ToString(), envelope.InitiatorId);
        Assert.Equal(context.SourceAddress?.ToString(), envelope.SourceAddress);
        Assert.Equal(context.DestinationAddress?.ToString(), envelope.DestinationAddress);
        Assert.Equal(context.ResponseAddress?.ToString(), envelope.ResponseAddress);
        Assert.Equal(context.FaultAddress?.ToString(), envelope.FaultAddress);
        Assert.Equal(context.SupportedMessageTypes, envelope.MessageType);
        Assert.Same(context.Message, envelope.Message);
        Assert.Equal(ProjectionTime.UtcDateTime + context.TimeToLive, envelope.ExpirationTime);
        Assert.Equal(context.SentTime, envelope.SentTime);
        Assert.Equal("metadata", envelope.Headers["ViciOne-Test"]);
        Assert.NotNull(envelope.Host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "one-utc-read-for-missing-sent-time")]
    public void MissingSentTime_CapturesExactlyOneUtcInstant()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime)
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1),
        };
        var context = new TestMessageContext(timeProvider);

        var envelope = new JsonMessageEnvelope(context, new TestMessage("payload"), ["urn:message:test"]);

        Assert.Equal(ProjectionTime.UtcDateTime, envelope.SentTime);
        Assert.Equal(ProjectionTime + TimeSpan.FromSeconds(1), timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "expired-overlay-has-no-serializer-grace")]
    public void Overlay_ProjectsAnExpiredTimeToLiveWithoutInventingAGracePeriod()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = CreateSendContext();
        context.TimeToLive = TimeSpan.FromSeconds(-30);
        context.SetTimeProvider(timeProvider);
        var envelope = new JsonMessageEnvelope
        {
            MessageType = ["urn:message:original"],
            Message = new TestMessage("payload"),
            ExpirationTime = ProjectionTime.UtcDateTime + TimeSpan.FromHours(1),
            Headers = new Dictionary<string, object?>
            {
                ["Preserved"] = "existing",
            },
        };

        envelope.Update(context);

        Assert.Equal(ProjectionTime.UtcDateTime - TimeSpan.FromSeconds(30), envelope.ExpirationTime);
        Assert.Equal("existing", envelope.Headers["Preserved"]);
        Assert.Equal("metadata", envelope.Headers["ViciOne-Test"]);
        Assert.Equal(envelope.MessageType, context.SupportedMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "zero-ttl-is-immediate")]
    public void Overlay_ProjectsZeroTimeToLiveAsImmediateExpiration()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = new TestSendContext<TestMessage>(new TestMessage("replacement"))
        {
            TimeToLive = TimeSpan.Zero,
        };
        context.SetTimeProvider(timeProvider);
        var envelope = new JsonMessageEnvelope
        {
            MessageType = ["urn:message:original"],
            Message = new TestMessage("original"),
        };

        envelope.Update(context);

        Assert.Equal(ProjectionTime.UtcDateTime, envelope.ExpirationTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "overlay-clock-snapshot")]
    public void Overlay_WithTimeToLiveAndMissingSentTimeUsesOneUtcSnapshot()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime)
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1),
        };
        var context = new TestSendContext<TestMessage>(new TestMessage("replacement"))
        {
            TimeToLive = TimeSpan.FromMinutes(2),
        };
        context.SetTimeProvider(timeProvider);
        var envelope = new JsonMessageEnvelope
        {
            MessageType = ["urn:message:original"],
            Message = new TestMessage("original"),
        };

        envelope.Update(context);

        Assert.Equal(ProjectionTime.UtcDateTime, envelope.SentTime);
        Assert.Equal(ProjectionTime.UtcDateTime + TimeSpan.FromMinutes(2), envelope.ExpirationTime);
        Assert.Equal(ProjectionTime + TimeSpan.FromSeconds(1), timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "overlay-preserves-absent-values")]
    public void Overlay_PreservesExistingOptionalMetadataThatTheSendContextDoesNotReplace()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = new TestSendContext<TestMessage>(new TestMessage("replacement"))
        {
            DestinationAddress = new Uri("loopback://new-destination"),
        };
        context.SetTimeProvider(timeProvider);
        var envelope = new JsonMessageEnvelope
        {
            MessageId = "existing-message-id",
            SourceAddress = "loopback://existing-source",
            ResponseAddress = "loopback://existing-response",
            FaultAddress = "loopback://existing-fault",
            ExpirationTime = ProjectionTime.UtcDateTime + TimeSpan.FromMinutes(2),
            SentTime = ProjectionTime.UtcDateTime - TimeSpan.FromMinutes(1),
            MessageType = ["urn:message:original"],
            Message = new TestMessage("original"),
        };

        envelope.Update(context);

        Assert.Equal("existing-message-id", envelope.MessageId);
        Assert.Equal("loopback://existing-source", envelope.SourceAddress);
        Assert.Equal("loopback://new-destination/", envelope.DestinationAddress);
        Assert.Equal("loopback://existing-response", envelope.ResponseAddress);
        Assert.Equal("loopback://existing-fault", envelope.FaultAddress);
        Assert.Equal(ProjectionTime.UtcDateTime + TimeSpan.FromMinutes(2), envelope.ExpirationTime);
        Assert.Equal(ProjectionTime.UtcDateTime - TimeSpan.FromMinutes(1), envelope.SentTime);
    }

    private static MessageSendContext<TestMessage> CreateSendContext()
    {
        var context = new MessageSendContext<TestMessage>(new TestMessage("payload"))
        {
            MessageId = Guid.Parse("d45e2fd1-37c8-4ee8-8544-0fa39afda4bb"),
            RequestId = Guid.Parse("7f28986b-e1f4-47bd-bce1-ef1936031f04"),
            CorrelationId = Guid.Parse("8e3b4d14-9325-4417-a12f-d65bb5a94d61"),
            ConversationId = Guid.Parse("cead71d9-bdbb-4b67-8206-72f7afea37a8"),
            InitiatorId = Guid.Parse("35a91ad7-b497-43a3-b93d-039405760ad2"),
            SourceAddress = new Uri("loopback://source"),
            DestinationAddress = new Uri("loopback://destination"),
            ResponseAddress = new Uri("loopback://response"),
            FaultAddress = new Uri("loopback://fault"),
        };
        context.Headers.Set("ViciOne-Test", "metadata");
        return context;
    }

    private sealed record TestMessage(string Value);

    private sealed class TestMessageContext : BasePipeContext, MessageContext
    {
        private readonly DictionarySendHeaders _headers = new();

        public TestMessageContext(TimeProvider timeProvider)
        {
            this.SetTimeProvider(timeProvider);
        }

        public Guid? MessageId => null;
        public Guid? RequestId => null;
        public Guid? CorrelationId => null;
        public Guid? ConversationId => null;
        public Guid? InitiatorId => null;
        public DateTime? ExpirationTime => null;
        public Uri? SourceAddress => null;
        public Uri? DestinationAddress => null;
        public Uri? ResponseAddress => null;
        public Uri? FaultAddress => null;
        public DateTime? SentTime => null;
        public Headers Headers => _headers;
        public HostInfo Host => HostMetadataCache.Host;
    }

    private sealed class TestSendContext<TMessage>(TMessage message) : BasePipeContext, SendContext<TMessage>
        where TMessage : class
    {
        private readonly DictionarySendHeaders _headers = new();

        public TMessage Message { get; } = message;
        public Uri? SourceAddress { get; set; }
        public Uri? DestinationAddress { get; set; }
        public Uri? ResponseAddress { get; set; }
        public Uri? FaultAddress { get; set; }
        public Guid? RequestId { get; set; }
        public Guid? MessageId { get; set; }
        public Guid? CorrelationId { get; set; }
        public Guid? ConversationId { get; set; }
        public Guid? InitiatorId { get; set; }
        public Guid? ScheduledMessageId { get; set; }
        public SendHeaders Headers => _headers;
        public TimeSpan? TimeToLive { get; set; }
        public DateTime? SentTime { get; init; }
        public System.Net.Mime.ContentType? ContentType { get; set; }
        public bool Durable { get; set; }
        public TimeSpan? Delay { get; set; }
        public IMessageSerializer Serializer { get; set; } = null!;
        public ISerialization Serialization { get; set; } = null!;
        public string[] SupportedMessageTypes { get; set; } = [];
        public long? BodyLength => null;

        public SendContext<T> CreateProxy<T>(T proxyMessage)
            where T : class => new TestSendContext<T>(proxyMessage);
    }
}
