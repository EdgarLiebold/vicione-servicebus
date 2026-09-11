using System.Net.Mime;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class SendContextScopeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "constructors-require-context-and-valid-payloads")]
    public void Constructors_RequireAContextAndValidPayloadCollection()
    {
        var parent = new TestSendContext<TestMessage>(new TestMessage());

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new SendContextScope(null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentNullException>(() => new SendContextScope(parent, (object[])null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentException>(() => new SendContextScope(parent, [null!])).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new SendContextScope<TestMessage>(null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentNullException>(() => new SendContextScope<TestMessage>(parent, (object[])null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentException>(() => new SendContextScope<TestMessage>(parent, [null!])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "self-local-parent-resolution-precedence")]
    public void PayloadResolution_UsesSelfThenLocalThenParentPrecedence()
    {
        var parentPayload = new NamedPayload("parent");
        var parentOnly = new ParentPayload("parent-only");
        var parent = new TestSendContext<TestMessage>(new TestMessage(), parentPayload, parentOnly);
        var localPayload = new NamedPayload("local");
        var scope = new SendContextScope(parent, localPayload);
        var factoryCalls = 0;

        Assert.True(scope.HasPayloadType(typeof(SendContextScope)));
        Assert.True(scope.TryGetPayload(out SendContextScope? self));
        Assert.Same(scope, self);
        Assert.Same(scope, scope.GetOrAddPayload(() =>
        {
            factoryCalls++;
            return new SendContextScope(parent);
        }));
        Assert.Same(scope, scope.AddOrUpdatePayload(
            () =>
            {
                factoryCalls++;
                return new SendContextScope(parent);
            },
            _ =>
            {
                factoryCalls++;
                return new SendContextScope(parent);
            }));

        Assert.True(scope.HasPayloadType(typeof(NamedPayload)));
        Assert.True(scope.TryGetPayload(out NamedPayload? local));
        Assert.Same(localPayload, local);
        Assert.Same(localPayload, scope.GetOrAddPayload(() =>
        {
            factoryCalls++;
            return new NamedPayload("unexpected");
        }));

        Assert.True(scope.HasPayloadType(typeof(ParentPayload)));
        Assert.True(scope.TryGetPayload(out ParentPayload? inherited));
        Assert.Same(parentOnly, inherited);
        Assert.Same(parentOnly, scope.GetOrAddPayload(() =>
        {
            factoryCalls++;
            return new ParentPayload("unexpected");
        }));
        Assert.False(scope.HasPayloadType(typeof(MissingPayload)));
        Assert.False(scope.TryGetPayload(out MissingPayload? missing));
        Assert.Null(missing);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(
            "payloadType",
            Assert.Throws<ArgumentNullException>(() => scope.HasPayloadType(null!)).ParamName);
        Assert.Equal(
            "payloadFactory",
            Assert.Throws<ArgumentNullException>(() => scope.GetOrAddPayload<MissingPayload>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "add-and-update-remain-local-to-scope")]
    public void PayloadMutation_UsesExactFactoryAndNeverMutatesTheParentCache()
    {
        var parentPayload = new ParentPayload("parent");
        var parent = new TestSendContext<TestMessage>(new TestMessage(), parentPayload);
        var localPayload = new NamedPayload("local");
        var scope = new SendContextScope(parent, localPayload);
        var addCalls = 0;
        var updateCalls = 0;

        NamedPayload updatedLocal = scope.AddOrUpdatePayload(
            () =>
            {
                addCalls++;
                return new NamedPayload("added");
            },
            current =>
            {
                updateCalls++;
                Assert.Same(localPayload, current);
                return new NamedPayload("updated-local");
            });
        ParentPayload copiedFromParent = scope.AddOrUpdatePayload(
            () =>
            {
                addCalls++;
                return new ParentPayload("added");
            },
            current =>
            {
                updateCalls++;
                Assert.Same(parentPayload, current);
                return new ParentPayload("scope-copy");
            });
        MissingPayload added = scope.AddOrUpdatePayload(
            () =>
            {
                addCalls++;
                return new MissingPayload("added");
            },
            _ =>
            {
                updateCalls++;
                return new MissingPayload("unexpected");
            });

        Assert.Equal("updated-local", updatedLocal.Value);
        Assert.Equal("scope-copy", copiedFromParent.Value);
        Assert.Equal("added", added.Value);
        Assert.Equal(1, addCalls);
        Assert.Equal(2, updateCalls);
        Assert.Same(updatedLocal, scope.GetOrAddPayload<NamedPayload>(() => throw new InvalidOperationException()));
        Assert.Same(copiedFromParent, scope.GetOrAddPayload<ParentPayload>(() => throw new InvalidOperationException()));
        Assert.Same(added, scope.GetOrAddPayload<MissingPayload>(() => throw new InvalidOperationException()));
        Assert.True(parent.TryGetPayload(out ParentPayload? unchangedParent));
        Assert.Same(parentPayload, unchangedParent);
        Assert.False(parent.TryGetPayload(out MissingPayload? _));
        Assert.Equal(
            "addFactory",
            Assert.Throws<ArgumentNullException>(() => scope.AddOrUpdatePayload<MissingPayload>(null!, _ => added)).ParamName);
        Assert.Equal(
            "updateFactory",
            Assert.Throws<ArgumentNullException>(() => scope.AddOrUpdatePayload(() => added, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "typed-message-cancellation-and-replacement-retain-local-scope")]
    public void TypedScopeAndReplacementView_PreserveMessageCancellationAndLocalPayloads()
    {
        using var cancellationSource = new CancellationTokenSource();
        var message = new TestMessage();
        var parent = new TestSendContext<TestMessage>(message, cancellationSource.Token);
        var localPayload = new NamedPayload("local");
        var typedScope = new SendContextScope<TestMessage>(parent, localPayload);
        var replacementMessage = new OtherMessage();

        SendContext<OtherMessage> replacement = typedScope.CreateProxy(replacementMessage);

        Assert.Same(message, typedScope.Message);
        Assert.Equal(cancellationSource.Token, typedScope.CancellationToken);
        Assert.Same(replacementMessage, replacement.Message);
        Assert.True(replacement.TryGetPayload(out NamedPayload? retained));
        Assert.Same(localPayload, retained);
        Assert.True(replacement.TryGetPayload(out SendContextScope<TestMessage>? retainedScope));
        Assert.Same(typedScope, retainedScope);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "payload-array-is-snapshotted-at-construction")]
    public void Constructor_SnapshotsTheCallerOwnedPayloadArray()
    {
        var parent = new TestSendContext<TestMessage>(new TestMessage());
        var original = new NamedPayload("original");
        object[] payloads = [original];
        var scope = new SendContextScope(parent, payloads);

        payloads[0] = new NamedPayload("replacement");

        Assert.True(scope.TryGetPayload(out NamedPayload? resolved));
        Assert.Same(original, resolved);
    }

    private sealed class TestSendContext<TMessage> :
        BasePipeContext,
        SendContext<TMessage>
        where TMessage : class
    {
        public TestSendContext(TMessage message, params object[] payloads)
            : base(payloads)
        {
            Message = message;
        }

        public TestSendContext(TMessage message, CancellationToken cancellationToken, params object[] payloads)
            : base(cancellationToken, payloads)
        {
            Message = message;
        }

        public TMessage Message { get; }
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
        public SendHeaders Headers { get; } = null!;
        public TimeSpan? TimeToLive { get; set; }
        public DateTimeOffset? SentTime { get; }
        public ContentType? ContentType { get; set; }
        public bool Durable { get; set; }
        public TimeSpan? Delay { get; set; }
        public IMessageSerializer Serializer { get; set; } = null!;
        public ISerialization Serialization { get; set; } = null!;
        public string[] SupportedMessageTypes { get; set; } = [];
        public long? BodyLength { get; }

        public SendContext<T> CreateProxy<T>(T message)
            where T : class => new SendContextProxy<T>(this, message);
    }

    private sealed record NamedPayload(string Value);
    private sealed record ParentPayload(string Value);
    private sealed record MissingPayload(string Value);
    private sealed record TestMessage;
    private sealed record OtherMessage;
}
