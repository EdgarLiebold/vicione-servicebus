using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackSerializerContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "outer-envelope-failure-is-observable")]
    public void Deserialize_RejectsMalformedEnvelopeBytes()
    {
        var serializer = new MessagePackMessageSerializer();

        Assert.Throws<MessagePackSerializationException>(() =>
            serializer.Deserialize(new BinaryMessageBody(new byte[] { 0xC1 }), EmptyHeaders.Instance));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "missing-arguments-have-exact-ownership")]
    public void Deserialize_RejectsMissingArgumentsWithExactOwnership()
    {
        var serializer = new MessagePackMessageSerializer();

        Assert.Equal("receiveContext", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize((ReceiveContext)null!)).ParamName);
        Assert.Equal("body", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize(null!, EmptyHeaders.Instance)).ParamName);
        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() =>
            serializer.Deserialize(new BinaryMessageBody(ReadOnlyMemory<byte>.Empty), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "unsupported-and-malformed-contracts-return-false")]
    public void TryGetMessage_ReturnsFalseForUnsupportedAndMalformedContracts()
    {
        RoundTripResult<ContextValue> roundTrip = MessagePackRoundTrip.ExecuteWithContext(
            new ContextValue { Id = 27, Name = "supported" });

        Assert.False(roundTrip.Context.TryGetMessage<OtherContextValue>(out var unsupported));
        Assert.Null(unsupported);
        Assert.False(roundTrip.Context.TryGetMessage(typeof(OtherContextValue), out var runtimeUnsupported));
        Assert.Null(runtimeUnsupported);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            roundTrip.Context.TryGetMessage(null!, out _)).ParamName);

        SerializerContext malformed = CreateContextWithPayload([0xC1]);
        Assert.False(malformed.TryGetMessage<ContextValue>(out var invalid));
        Assert.Null(invalid);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "cancellation-remains-observable")]
    public void TryGetMessage_DoesNotConvertCancellationIntoAnUnsupportedContract()
    {
        var serializer = new MessagePackMessageSerializer();
        var message = new CancellationMessage();
        var sendContext = new MessageSendContext<CancellationMessage>(message);
        var envelope = new MessagePackEnvelope(sendContext, message);
        byte[] envelopeBytes = MessagePackSerializationRuntime.Serialize(envelope);
        SerializerContext context = serializer.Deserialize(
            new BinaryMessageBody(envelopeBytes),
            EmptyHeaders.Instance);

        Assert.Throws<OperationCanceledException>(() =>
            context.TryGetMessage<CancellationMessage>(out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "nested-pure-versus-mixed-cancellation-callback")]
    public void TryGetMessage_PropagatesOnlyPureNestedCancellationFromDeserializationCallbacks()
    {
        SerializerContext pure = CreateContextFor(new PureCancellationMessage());
        SerializerContext mixed = CreateContextFor(new MixedFailureMessage());

        OperationCanceledException cancellation = Assert.ThrowsAny<OperationCanceledException>(() =>
            pure.TryGetMessage<PureCancellationMessage>(out _));
        Assert.Equal("first cancellation", cancellation.Message);

        Assert.False(mixed.TryGetMessage<MixedFailureMessage>(out var result));
        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "cyclic-non-cancellation-branch-is-not-cancellation")]
    public void TryGetMessage_CyclicBusinessBranchCannotTurnAnAggregateIntoCancellation()
    {
        SerializerContext context = CreateContextFor(new CyclicFailureMessage());

        Assert.False(context.TryGetMessage<CyclicFailureMessage>(out var result));
        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "shared-cancellation-leaf-remains-cancellation")]
    public void TryGetMessage_SharedCancellationLeafIsNotMistakenForACycle()
    {
        SerializerContext context = CreateContextFor(new SharedCancellationMessage());

        OperationCanceledException actual = Assert.ThrowsAny<OperationCanceledException>(() =>
            context.TryGetMessage<SharedCancellationMessage>(out _));

        Assert.Equal("shared cancellation", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "cancellation-with-business-inner-is-mixed")]
    public void TryGetMessage_CancellationWithBusinessInnerIsNotPureCancellation()
    {
        SerializerContext context = CreateContextFor(new CancellationWithBusinessInnerMessage());

        Assert.False(context.TryGetMessage<CancellationWithBusinessInnerMessage>(out var result));
        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "empty-aggregate-prevents-false-cancellation")]
    public void TryGetMessage_EmptyAggregateBranchCannotTurnIntoCancellation()
    {
        SerializerContext context = CreateContextFor(new EmptyAggregateBranchMessage());

        Assert.False(context.TryGetMessage<EmptyAggregateBranchMessage>(out var result));
        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "throwing-base-exception-prevents-false-cancellation")]
    public void TryGetMessage_ThrowingBaseExceptionBranchCannotTurnIntoCancellation()
    {
        SerializerContext context = CreateContextFor(new ThrowingBaseExceptionBranchMessage());

        Assert.False(context.TryGetMessage<ThrowingBaseExceptionBranchMessage>(out var result));
        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "non-null-envelope-payload")]
    public void SerializerContext_RejectsEveryMissingOwnedInputAndPayload()
    {
        var serializer = new MessagePackMessageSerializer();
        var sendContext = new MessageSendContext<ContextValue>(new ContextValue());
        var envelope = new MessagePackEnvelope(sendContext, sendContext.Message);
        var messageContext = new EnvelopeMessageContext(envelope, serializer);

        Assert.Equal("serializer", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackSerializerContext(null!, messageContext, [], envelope)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackSerializerContext(serializer, null!, [], envelope)).ParamName);
        Assert.Equal("supportedMessageTypes", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackSerializerContext(serializer, messageContext, null!, envelope)).ParamName);
        Assert.Equal("envelope", Assert.Throws<ArgumentNullException>(() =>
            new MessagePackSerializerContext(serializer, messageContext, [], null!)).ParamName);

        envelope.MessageTypes = null;
        SerializerContext contextWithoutDeclaredTypes = serializer.Deserialize(
            new BinaryMessageBody(MessagePackSerializationRuntime.Serialize(envelope)),
            EmptyHeaders.Instance);

        Assert.Empty(contextWithoutDeclaredTypes.SupportedMessageTypes);

        envelope.Message = null;
        var exception = Assert.Throws<ArgumentException>(() =>
            new MessagePackSerializerContext(serializer, messageContext, [], envelope));

        Assert.Equal("envelope", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DESERIALIZATION", "dictionary-and-explicit-contract-projection")]
    public void SerializerContext_ProjectsDictionariesAndExplicitContractSets()
    {
        var message = new ContextValue { Id = 27, Name = "Frank" };
        RoundTripResult<ContextValue> source = MessagePackRoundTrip.ExecuteWithContext(message);

        Dictionary<string, object> values = source.Context.ToDictionary(message);
        Dictionary<string, object> empty = source.Context.ToDictionary<ContextValue>(null);
        IMessageSerializer forwarding = source.Context.GetMessageSerializer(
            message,
            [MessageUrn.ForTypeString<ContextValue>(), "urn:message:explicit-context-value"]);
        var sendContext = new MessageSendContext<ContextValue>(message);
        MessageBody body = forwarding.GetMessageBody(sendContext);
        SerializerContext result = new MessagePackMessageSerializer()
            .Deserialize(body, EmptyHeaders.Instance);

        Assert.Equal(27, source.Context.DeserializeObject<int>(values["ID"]));
        Assert.Equal("Frank", values["name"]);
        Assert.Empty(empty);
        Assert.Contains("urn:message:explicit-context-value", result.SupportedMessageTypes);
        Assert.True(result.TryGetMessage<ContextValue>(out var restored));
        Assert.Equal(27, restored.Id);
        Assert.Equal("Frank", restored.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "typed-overlay-preserves-existing-properties")]
    public void TypedForwardingSerializer_OverlaysReplacementValuesAndPreservesTheRemainingPayload()
    {
        var original = new ContextValue { Id = 27, Name = "original" };
        var originalSendContext = new MessageSendContext<ContextValue>(original);
        var envelope = new MessagePackEnvelope(originalSendContext, original);
        SerializerContext source = new MessagePackMessageSerializer().Deserialize(
            new MessagePackMessageBody<ContextValue>(originalSendContext, envelope),
            EmptyHeaders.Instance);
        IMessageSerializer forwarding = source.GetMessageSerializer(
            envelope,
            new ContextNameOverlay { Name = "updated" });

        var forwardedSendContext = new MessageSendContext<ContextValue>(original);
        SerializerContext result = new MessagePackMessageSerializer().Deserialize(
            forwarding.GetMessageBody(forwardedSendContext),
            EmptyHeaders.Instance);

        Assert.True(result.TryGetMessage<ContextValue>(out var restored));
        Assert.Equal(27, restored.Id);
        Assert.Equal("updated", restored.Name);
    }

    private static SerializerContext CreateContextWithPayload(byte[] payload)
    {
        var serializer = new MessagePackMessageSerializer();
        var sendContext = new MessageSendContext<ContextValue>(new ContextValue());
        var envelope = new MessagePackEnvelope(sendContext, sendContext.Message)
        {
            IsNativeMessagePackPayload = true,
            Message = payload,
        };
        byte[] envelopeBytes = MessagePackSerializationRuntime.Serialize(envelope);

        return serializer.Deserialize(new BinaryMessageBody(envelopeBytes), EmptyHeaders.Instance);
    }

    private static SerializerContext CreateContextFor<T>(T message) where T : class
    {
        var serializer = new MessagePackMessageSerializer();
        var sendContext = new MessageSendContext<T>(message);
        var envelope = new MessagePackEnvelope(sendContext, message);
        return serializer.Deserialize(
            new BinaryMessageBody(MessagePackSerializationRuntime.Serialize(envelope)),
            EmptyHeaders.Instance);
    }

    private sealed class ContextValue
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class OtherContextValue;

    private sealed class CancellationMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new OperationCanceledException("Deserialization canceled.");
    }

    private sealed class PureCancellationMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new InvalidOperationException("outer", new AggregateException(
            new OperationCanceledException("first cancellation"), new OperationCanceledException("second cancellation")));
    }

    private sealed class MixedFailureMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new InvalidOperationException("outer", new AggregateException(
            new OperationCanceledException("first cancellation"), new InvalidOperationException("business failure")));
    }

    private sealed class CyclicFailureMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            var first = new CyclicBaseFailure();
            var second = new CyclicBaseFailure();
            first.Base = second;
            second.Base = first;
            throw new InvalidOperationException("outer", new AggregateException(
                new OperationCanceledException("canceled branch"), first));
        }
    }

    private sealed class SharedCancellationMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            var cancellation = new OperationCanceledException("shared cancellation");
            throw new InvalidOperationException("outer", new AggregateException(cancellation, cancellation));
        }
    }

    private sealed class CancellationWithBusinessInnerMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new AggregateException(
            new OperationCanceledException("first cancellation"),
            new OperationCanceledException("second cancellation", new InvalidOperationException("business failure")));
    }

    private sealed class EmptyAggregateBranchMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new AggregateException(
            new AggregateException(), new OperationCanceledException("canceled branch"));
    }

    private sealed class ThrowingBaseExceptionBranchMessage : IMessagePackSerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize() => throw new AggregateException(
            new ThrowingBaseException(), new OperationCanceledException("canceled branch"));
    }

    private sealed class ThrowingBaseException : Exception
    {
        public override Exception GetBaseException() => throw new InvalidOperationException("base inspection failed");
    }

    private sealed class CyclicBaseFailure : Exception
    {
        public Exception? Base { get; set; }

        public override Exception GetBaseException() => Base!;
    }

    private sealed class ContextNameOverlay
    {
        public string Name { get; set; } = string.Empty;
    }
}
