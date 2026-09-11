using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class PublishContextProxyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-PROXY", "publish-proxy-requires-context-and-message")]
    public void Constructor_RequiresContextAndMessage()
    {
        PublishContext context = CreateContext(out _);
        var message = new TestMessage();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new PublishContextProxy<TestMessage>(null!, message)).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new PublishContextProxy<TestMessage>(context, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-PROXY", "publish-proxy-forwards-complete-context-state")]
    public void Properties_ForwardCompletePublishStateInBothDirections()
    {
        PublishContext context = CreateContext(out RecordingContextProxy recorder);
        var message = new TestMessage();
        var proxy = new PublishContextProxy<TestMessage>(context, message);

        Assert.Same(message, proxy.Message);
        Assert.Equal(recorder.CancellationToken, proxy.CancellationToken);
        Assert.Same(recorder.SourceAddress, proxy.SourceAddress);
        Assert.Same(recorder.DestinationAddress, proxy.DestinationAddress);
        Assert.Same(recorder.ResponseAddress, proxy.ResponseAddress);
        Assert.Same(recorder.FaultAddress, proxy.FaultAddress);
        Assert.Equal(recorder.RequestId, proxy.RequestId);
        Assert.Equal(recorder.MessageId, proxy.MessageId);
        Assert.Equal(recorder.CorrelationId, proxy.CorrelationId);
        Assert.Equal(recorder.ConversationId, proxy.ConversationId);
        Assert.Equal(recorder.InitiatorId, proxy.InitiatorId);
        Assert.Equal(recorder.ScheduledMessageId, proxy.ScheduledMessageId);
        Assert.Same(recorder.Headers, proxy.Headers);
        Assert.Equal(recorder.TimeToLive, proxy.TimeToLive);
        Assert.Equal(recorder.SentTime, proxy.SentTime);
        Assert.Same(recorder.ContentType, proxy.ContentType);
        Assert.Equal(recorder.Durable, proxy.Durable);
        Assert.Equal(recorder.Delay, proxy.Delay);
        Assert.Same(recorder.Serializer, proxy.Serializer);
        Assert.Same(recorder.Serialization, proxy.Serialization);
        Assert.Same(recorder.SupportedMessageTypes, proxy.SupportedMessageTypes);
        Assert.Equal(recorder.BodyLength, proxy.BodyLength);
        Assert.Equal(recorder.Mandatory, proxy.Mandatory);

        var sourceAddress = new Uri("loopback://updated/source");
        var destinationAddress = new Uri("loopback://updated/destination");
        var responseAddress = new Uri("loopback://updated/response");
        var faultAddress = new Uri("loopback://updated/fault");
        Guid requestId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid messageId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        Guid correlationId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        Guid conversationId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        Guid initiatorId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        Guid scheduledMessageId = Guid.Parse("60000000-0000-0000-0000-000000000006");
        TimeSpan lifetime = TimeSpan.FromMinutes(8);
        var contentType = new ContentType("application/vnd.vicione.updated");
        TimeSpan delay = TimeSpan.FromSeconds(17);
        IMessageSerializer serializer = CreateProxy<IMessageSerializer>();
        ISerialization serialization = CreateProxy<ISerialization>();
        string[] supportedTypes = ["urn:message:updated"];

        proxy.SourceAddress = sourceAddress;
        proxy.DestinationAddress = destinationAddress;
        proxy.ResponseAddress = responseAddress;
        proxy.FaultAddress = faultAddress;
        proxy.RequestId = requestId;
        proxy.MessageId = messageId;
        proxy.CorrelationId = correlationId;
        proxy.ConversationId = conversationId;
        proxy.InitiatorId = initiatorId;
        proxy.ScheduledMessageId = scheduledMessageId;
        proxy.TimeToLive = lifetime;
        proxy.ContentType = contentType;
        proxy.Durable = false;
        proxy.Delay = delay;
        proxy.Serializer = serializer;
        proxy.Serialization = serialization;
        proxy.SupportedMessageTypes = supportedTypes;
        proxy.Mandatory = false;

        Assert.Same(sourceAddress, recorder.SourceAddress);
        Assert.Same(destinationAddress, recorder.DestinationAddress);
        Assert.Same(responseAddress, recorder.ResponseAddress);
        Assert.Same(faultAddress, recorder.FaultAddress);
        Assert.Equal(requestId, recorder.RequestId);
        Assert.Equal(messageId, recorder.MessageId);
        Assert.Equal(correlationId, recorder.CorrelationId);
        Assert.Equal(conversationId, recorder.ConversationId);
        Assert.Equal(initiatorId, recorder.InitiatorId);
        Assert.Equal(scheduledMessageId, recorder.ScheduledMessageId);
        Assert.Equal(lifetime, recorder.TimeToLive);
        Assert.Same(contentType, recorder.ContentType);
        Assert.False(recorder.Durable);
        Assert.Equal(delay, recorder.Delay);
        Assert.Same(serializer, recorder.Serializer);
        Assert.Same(serialization, recorder.Serialization);
        Assert.Same(supportedTypes, recorder.SupportedMessageTypes);
        Assert.False(recorder.Mandatory);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-PROXY", "publish-replacement-view-preserves-current-proxy")]
    public void CreateProxy_PreservesTheCurrentPublishViewAndRequiresAMessage()
    {
        PublishContext context = CreateContext(out RecordingContextProxy recorder);
        var current = new PublishContextProxy<TestMessage>(context, new TestMessage());
        var replacementMessage = new OtherMessage();

        SendContext<OtherMessage> replacement = current.CreateProxy(replacementMessage);

        var publishReplacement = Assert.IsType<PublishContextProxy<OtherMessage>>(replacement);
        Assert.Same(replacementMessage, publishReplacement.Message);
        Assert.True(publishReplacement.TryGetPayload(out PublishContextProxy<TestMessage>? retained));
        Assert.Same(current, retained);
        Assert.Equal(0, recorder.CreateProxyCalls);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => current.CreateProxy<OtherMessage>(null!)).ParamName);
    }

    private static PublishContext CreateContext(out RecordingContextProxy recorder)
    {
        PublishContext context = DispatchProxy.Create<PublishContext, RecordingContextProxy>();
        recorder = (RecordingContextProxy)(object)context;
        recorder.Initialize();
        return context;
    }

    private static TContract CreateProxy<TContract>()
        where TContract : class => DispatchProxy.Create<TContract, UnusedProxy>();

    private class RecordingContextProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _properties = [];

        public CancellationToken CancellationToken => Get<CancellationToken>(nameof(PipeContext.CancellationToken));
        public Uri SourceAddress => Get<Uri>(nameof(SendContext.SourceAddress));
        public Uri DestinationAddress => Get<Uri>(nameof(SendContext.DestinationAddress));
        public Uri ResponseAddress => Get<Uri>(nameof(SendContext.ResponseAddress));
        public Uri FaultAddress => Get<Uri>(nameof(SendContext.FaultAddress));
        public Guid? RequestId => Get<Guid?>(nameof(SendContext.RequestId));
        public Guid? MessageId => Get<Guid?>(nameof(SendContext.MessageId));
        public Guid? CorrelationId => Get<Guid?>(nameof(SendContext.CorrelationId));
        public Guid? ConversationId => Get<Guid?>(nameof(SendContext.ConversationId));
        public Guid? InitiatorId => Get<Guid?>(nameof(SendContext.InitiatorId));
        public Guid? ScheduledMessageId => Get<Guid?>(nameof(SendContext.ScheduledMessageId));
        public SendHeaders Headers => Get<SendHeaders>(nameof(SendContext.Headers));
        public TimeSpan? TimeToLive => Get<TimeSpan?>(nameof(SendContext.TimeToLive));
        public DateTimeOffset? SentTime => Get<DateTimeOffset?>(nameof(SendContext.SentTime));
        public ContentType ContentType => Get<ContentType>(nameof(SendContext.ContentType));
        public bool Durable => Get<bool>(nameof(SendContext.Durable));
        public TimeSpan? Delay => Get<TimeSpan?>(nameof(SendContext.Delay));
        public IMessageSerializer Serializer => Get<IMessageSerializer>(nameof(SendContext.Serializer));
        public ISerialization Serialization => Get<ISerialization>(nameof(SendContext.Serialization));
        public string[] SupportedMessageTypes => Get<string[]>(nameof(SendContext.SupportedMessageTypes));
        public long? BodyLength => Get<long?>(nameof(SendContext.BodyLength));
        public bool Mandatory => Get<bool>(nameof(PublishContext.Mandatory));

        public int CreateProxyCalls { get; private set; }

        public void Initialize()
        {
            _properties[nameof(PipeContext.CancellationToken)] = TestContext.Current.CancellationToken;
            _properties[nameof(SendContext.SourceAddress)] = new Uri("loopback://original/source");
            _properties[nameof(SendContext.DestinationAddress)] = new Uri("loopback://original/destination");
            _properties[nameof(SendContext.ResponseAddress)] = new Uri("loopback://original/response");
            _properties[nameof(SendContext.FaultAddress)] = new Uri("loopback://original/fault");
            _properties[nameof(SendContext.RequestId)] = Guid.Parse("01000000-0000-0000-0000-000000000001");
            _properties[nameof(SendContext.MessageId)] = Guid.Parse("02000000-0000-0000-0000-000000000002");
            _properties[nameof(SendContext.CorrelationId)] = Guid.Parse("03000000-0000-0000-0000-000000000003");
            _properties[nameof(SendContext.ConversationId)] = Guid.Parse("04000000-0000-0000-0000-000000000004");
            _properties[nameof(SendContext.InitiatorId)] = Guid.Parse("05000000-0000-0000-0000-000000000005");
            _properties[nameof(SendContext.ScheduledMessageId)] = Guid.Parse("06000000-0000-0000-0000-000000000006");
            _properties[nameof(SendContext.Headers)] = CreateProxy<SendHeaders>();
            _properties[nameof(SendContext.TimeToLive)] = TimeSpan.FromMinutes(3);
            _properties[nameof(SendContext.SentTime)] = new DateTimeOffset(2026, 9, 11, 9, 30, 0, TimeSpan.Zero);
            _properties[nameof(SendContext.ContentType)] = new ContentType("application/vnd.vicione.original");
            _properties[nameof(SendContext.Durable)] = true;
            _properties[nameof(SendContext.Delay)] = TimeSpan.FromSeconds(5);
            _properties[nameof(SendContext.Serializer)] = CreateProxy<IMessageSerializer>();
            _properties[nameof(SendContext.Serialization)] = CreateProxy<ISerialization>();
            _properties[nameof(SendContext.SupportedMessageTypes)] = new[] { "urn:message:original" };
            _properties[nameof(SendContext.BodyLength)] = 1_024L;
            _properties[nameof(PublishContext.Mandatory)] = true;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal))
                return _properties[targetMethod.Name[4..]];
            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                _properties[targetMethod.Name[4..]] = args![0];
                return null;
            }
            if (targetMethod.Name == nameof(SendContext.CreateProxy))
            {
                CreateProxyCalls++;
                throw new InvalidOperationException("The replacement view bypassed the current proxy.");
            }

            throw new NotSupportedException(targetMethod.Name);
        }

        private T Get<T>(string propertyName) => (T)_properties[propertyName]!;
    }

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed record TestMessage;

    private sealed record OtherMessage;
}
