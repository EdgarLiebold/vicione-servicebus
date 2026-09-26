using System.Buffers;
using System.IO;
using System.Net.Mime;
using System.Text;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class BoundedSerializerAdmissionContractTests
{
    private static readonly ContentType BinaryContentType = new("application/octet-stream");

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "bounded-serializer-exact-body-and-envelope")]
    public void BoundedSerializer_AdmitsExactBodyAndEnvelopeAndSnapshotsRetainedWriters()
    {
        var admission = Admission(maximumBodyBytes: 3, maximumEnvelopeBytes: 5);
        var serializer = new TestBoundedSerializer { BodyBytes = "ABC"u8.ToArray() };

        BoundedSerializerMessageBody body = Create(serializer, admission);

        Assert.Equal(1, serializer.BodyWrites);
        Assert.Equal(1, serializer.EnvelopeWrites);
        Assert.Equal(1, serializer.LocatorCalls);
        Assert.False(serializer.BodyStreamWasWritable);
        Assert.Equal(0, serializer.BodyStreamInitialPosition);
        Assert.Equal("ABC"u8.ToArray(), serializer.ObservedSerializedBody);
        Assert.Equal("<ABC>"u8.ToArray(), serializer.ObservedEnvelope);
        Assert.Equal(5, body.Length);
        Assert.True(admission.HasCompleteAdmissionFor(body.Length));
        Assert.True(admission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));
        Assert.True(proof.MatchesEnvelope(body.ToArray(), BinaryContentType.ToString()));

        serializer.RetainedBodyMemory.Span.Fill((byte)'X');
        serializer.RetainedEnvelopeMemory.Span.Fill((byte)'Y');
        Assert.Equal("<ABC>"u8.ToArray(), body.ToArray());
        Assert.True(proof.MatchesEnvelope(body.ToArray(), BinaryContentType.ToString()));

        byte[] exposed = body.ToArray();
        exposed[0] = (byte)'Z';
        Assert.Equal("<ABC>"u8.ToArray(), body.ToArray());
        using Stream stream = body.OpenReadStream();
        Assert.False(stream.CanWrite);
        Assert.Equal(0, stream.Position);
        Assert.Equal("<ABC>"u8.ToArray(), ReadAll(stream));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "bounded-serializer-plus-one-stops-before-envelope")]
    public void BoundedSerializer_RejectsBodyPlusOneBeforeEnvelopeCallback()
    {
        var admission = Admission(maximumBodyBytes: 3, maximumEnvelopeBytes: 5);
        var serializer = new TestBoundedSerializer { BodyBytes = "ABCD"u8.ToArray() };

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() => Create(serializer, admission));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
        Assert.Equal(4, exception.ActualBytes);
        Assert.Equal(3, exception.ConfiguredLimitBytes);
        Assert.Equal(1, serializer.BodyWrites);
        Assert.Equal(0, serializer.EnvelopeWrites);
        Assert.Equal(0, serializer.LocatorCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "bounded-serializer-plus-one-stops-before-locator")]
    public void BoundedSerializer_RejectsEnvelopePlusOneBeforeLocatorCallback()
    {
        var admission = Admission(maximumBodyBytes: 3, maximumEnvelopeBytes: 5);
        var serializer = new TestBoundedSerializer { BodyBytes = "ABC"u8.ToArray(), Suffix = ">>"u8.ToArray() };

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() => Create(serializer, admission));

        Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
        Assert.Equal(6, exception.ActualBytes);
        Assert.Equal(5, exception.ConfiguredLimitBytes);
        Assert.Equal(1, serializer.BodyWrites);
        Assert.Equal(1, serializer.EnvelopeWrites);
        Assert.Equal(0, serializer.LocatorCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BOUNDED-WRITER", "hostile-serializer-reservation-rejected")]
    public void BoundedSerializer_RejectsHostileBodyReservationBeforeEnvelopeCallback()
    {
        var admission = Admission(maximumBodyBytes: 3, maximumEnvelopeBytes: 5);
        var serializer = new TestBoundedSerializer { BodyReservationHint = 4 };

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() => Create(serializer, admission));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
        Assert.Equal(4, exception.ActualBytes);
        Assert.Equal(0, serializer.EnvelopeWrites);
    }

    [Theory]
    [InlineData(false, 1, 3)]
    [InlineData(true, -1, 3)]
    [InlineData(true, 1, -1)]
    [InlineData(true, 3, 3)]
    [InlineData(true, 1, 2)]
    [InlineData(true, 0, 3)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "bounded-serializer-exact-contiguous-body-range")]
    public void BoundedSerializer_RejectsMissingInvalidOrNonmatchingBodyRange(bool found, int offset, int length)
    {
        var serializer = new TestBoundedSerializer
        {
            BodyBytes = "ABC"u8.ToArray(),
            LocatedRange = (found, offset, length),
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => Create(serializer, Admission(3, 5)));

        Assert.Contains("exact admitted application body", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, serializer.LocatorCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "bounded-serializer-transformed-body-rejected")]
    public void BoundedSerializer_RejectsAnEnvelopeThatTransformsTheAdmittedBody()
    {
        var serializer = new TestBoundedSerializer
        {
            BodyBytes = "ABC"u8.ToArray(),
            EnvelopeBodyOverride = "ABD"u8.ToArray(),
        };

        Assert.Throws<InvalidOperationException>(() => Create(serializer, Admission(3, 5)));
        Assert.Equal("ABC"u8.ToArray(), serializer.ObservedSerializedBody);
        Assert.Equal("<ABD>"u8.ToArray(), serializer.ObservedEnvelope);
    }

    [Theory]
    [InlineData(SerializedTransportTextFormat.None, false, null)]
    [InlineData(SerializedTransportTextFormat.Utf8, true, "<ABC>")]
    [InlineData(SerializedTransportTextFormat.Base64, true, "PEFCQz4=")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "lossless-text-carrier-selection")]
    public void BoundedSerializer_UsesTheDeclaredTransportTextFormat(
        SerializedTransportTextFormat format, bool available, string? expectedText)
    {
        var serializer = new TestBoundedSerializer { BodyBytes = "ABC"u8.ToArray(), TransportTextFormat = format };

        BoundedSerializerMessageBody body = Create(serializer, Admission(3, 5));

        Assert.Equal(available, body.TryGetTransportText(out string? text));
        Assert.Equal(expectedText, text);
        if (available)
            Assert.Equal(body.ToArray(), format == SerializedTransportTextFormat.Utf8
                ? Encoding.UTF8.GetBytes(text!)
                : Convert.FromBase64String(text!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "unknown-text-format-before-callbacks")]
    public void BoundedSerializer_RejectsUnknownTextFormatBeforeWriting()
    {
        var serializer = new TestBoundedSerializer { TransportTextFormat = (SerializedTransportTextFormat)17 };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Create(serializer, Admission(3, 5)));

        Assert.Equal("serializer", exception.ParamName);
        Assert.Equal(0, serializer.BodyWrites);
        Assert.Equal(0, serializer.EnvelopeWrites);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "utf8-carrier-rejects-invalid-bytes")]
    public void BoundedSerializer_RejectsInvalidUtf8WhenUtf8CarrierIsDeclared()
    {
        var serializer = new TestBoundedSerializer
        {
            BodyBytes = [0xff],
            Prefix = [],
            Suffix = [],
            TransportTextFormat = SerializedTransportTextFormat.Utf8,
        };

        Assert.Throws<DecoderFallbackException>(() => Create(serializer, Admission(1, 1)));
        Assert.Equal(1, serializer.LocatorCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "copied-envelope-locator-precise-body-and-snapshot")]
    public void CopiedEnvelopeLocator_AdmitsOnlyTheSelectedBodyAndSnapshotsSource()
    {
        byte[] source = "<ABC>"u8.ToArray();
        var admission = Admission(maximumBodyBytes: 3, maximumEnvelopeBytes: 5);
        var locator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };

        AdmittedCopyMessageBody body = AdmittedCopyMessageBody.Create(
            source.AsMemory(), BinaryContentType, Serialization(locator), admission, durableProof: null);

        Assert.Equal(1, locator.Calls);
        Assert.Equal("<ABC>"u8.ToArray(), locator.ObservedEnvelope);
        Assert.Equal(5, body.Length);
        Assert.True(admission.HasCompleteAdmissionFor(5));
        Assert.True(admission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));
        Assert.Equal(3, proof.SerializedBodyBytes);
        source[1] = (byte)'X';
        Assert.Equal("<ABC>"u8.ToArray(), body.ToArray());
        Assert.True(proof.MatchesEnvelope(body.ToArray(), BinaryContentType.ToString()));
    }

    [Theory]
    [InlineData(false, 1, 3)]
    [InlineData(true, -1, 3)]
    [InlineData(true, 1, -1)]
    [InlineData(true, 3, 3)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "copied-envelope-locator-range-validation")]
    public void CopiedEnvelopeLocator_RejectsMissingOrOutOfRangeBody(bool found, int offset, int length)
    {
        var locator = new TestCopiedLocator { LocatedRange = (found, offset, length) };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => AdmittedCopyMessageBody.Create(
            "<ABC>"u8.ToArray().AsMemory(), BinaryContentType, Serialization(locator), Admission(3, 5), durableProof: null));

        Assert.Contains("valid serialized application-body range", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, locator.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "durable-copy-replay-retains-admitted-body-size")]
    public void DurableCopiedEnvelope_ReplaysTheProvenBodyWithoutCallingAnUntrustedLocator()
    {
        byte[] source = "<ABC>"u8.ToArray();
        var originalAdmission = Admission(3, 5);
        var originalLocator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };
        _ = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(originalLocator), originalAdmission, durableProof: null);
        Assert.True(originalAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));

        var replayAdmission = Admission(3, 5);
        var invalidLocator = new TestCopiedLocator { LocatedRange = (false, -1, -1) };
        AdmittedCopyMessageBody replay = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(invalidLocator), replayAdmission, proof);

        Assert.Equal(0, invalidLocator.Calls);
        Assert.Equal("<ABC>"u8.ToArray(), replay.ToArray());
        Assert.True(replayAdmission.HasCompleteAdmissionFor(5));
        Assert.True(replayAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof replayProof));
        Assert.Equal(3, replayProof.SerializedBodyBytes);
        Assert.True(replayProof.MatchesEnvelope(replay.ToArray(), BinaryContentType.ToString()));
    }

    [Theory]
    [InlineData("body")]
    [InlineData("envelope")]
    [InlineData("content-type")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "durable-copy-replay-rejects-altered-proof-input")]
    public void DurableCopiedEnvelope_RejectsChangedBytesOrContentTypeBeforeReplay(string change)
    {
        byte[] source = "<ABC>"u8.ToArray();
        var originalAdmission = Admission(3, 5);
        var locator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };
        _ = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(locator), originalAdmission, durableProof: null);
        Assert.True(originalAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));

        var replayAdmission = Admission(5, 5);
        if (change == "body")
            source[2] = (byte)'X';
        if (change == "envelope")
            source[4] = (byte)'!';
        string persistedContentType = change == "content-type"
            ? "application/vnd.example.changed"
            : BinaryContentType.ToString();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                Serialization(locator), replayAdmission, proof, persistedContentType));

        Assert.Contains("does not match the copied envelope", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, locator.Calls);
        Assert.False(replayAdmission.HasCompleteAdmissionFor(source.Length));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "durable-copy-replay-uses-proven-offload-evidence")]
    public void DurableCopiedEnvelope_UsesProvenOffloadEvidenceUnderCurrentThreshold(bool originalOffloadObserved)
    {
        byte[] source = "<ABC>"u8.ToArray();
        var originalAdmission = Admission(3, 5, messageDataOffloadObserved: originalOffloadObserved);
        var locator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };
        _ = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(locator), originalAdmission, durableProof: null);
        Assert.True(originalAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));
        Assert.Equal(originalOffloadObserved, proof.MessageDataOffloadObserved);

        var replayAdmission = Admission(3, 5, messageDataOffloadObserved: !originalOffloadObserved,
            messageDataThresholdBytes: 2);
        if (originalOffloadObserved)
        {
            AdmittedCopyMessageBody replay = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                Serialization(locator), replayAdmission, proof);
            Assert.Equal(source, replay.ToArray());
            Assert.True(replayAdmission.HasCompleteAdmissionFor(source.Length));
        }
        else
        {
            PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() =>
                AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                    Serialization(locator), replayAdmission, proof));
            Assert.Equal(PayloadAdmissionStage.MessageData, exception.Stage);
            Assert.Equal(3, exception.ActualBytes);
            Assert.Equal(2, exception.ConfiguredLimitBytes);
            Assert.False(replayAdmission.HasCompleteAdmissionFor(source.Length));
        }
        Assert.Equal(1, locator.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "durable-copy-replay-preserves-offload-proof-across-generations")]
    public void DurableCopiedEnvelope_PreservesProvenOffloadEvidenceAcrossReplayGenerations(bool originalOffloadObserved)
    {
        byte[] source = "<ABC>"u8.ToArray();
        var originalAdmission = Admission(3, 5, messageDataOffloadObserved: originalOffloadObserved);
        var locator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };
        _ = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(locator), originalAdmission, durableProof: null);
        Assert.True(originalAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof originalProof));

        var firstReplayAdmission = Admission(3, 5, messageDataOffloadObserved: !originalOffloadObserved);
        AdmittedCopyMessageBody firstReplay = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(locator), firstReplayAdmission, originalProof);
        Assert.Equal(source, firstReplay.ToArray());
        Assert.True(firstReplayAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof replayProof));
        Assert.Equal(originalOffloadObserved, replayProof.MessageDataOffloadObserved);
        Assert.Equal(3, replayProof.SerializedBodyBytes);
        Assert.True(replayProof.MatchesEnvelope(source, BinaryContentType.ToString()));

        var secondReplayAdmission = Admission(3, 5, messageDataOffloadObserved: !originalOffloadObserved,
            messageDataThresholdBytes: 2);
        if (originalOffloadObserved)
        {
            AdmittedCopyMessageBody secondReplay = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                Serialization(locator), secondReplayAdmission, replayProof);
            Assert.Equal(source, secondReplay.ToArray());
            Assert.True(secondReplayAdmission.HasCompleteAdmissionFor(source.Length));
        }
        else
        {
            PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() =>
                AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                    Serialization(locator), secondReplayAdmission, replayProof));
            Assert.Equal(PayloadAdmissionStage.MessageData, exception.Stage);
            Assert.Equal(3, exception.ActualBytes);
            Assert.Equal(2, exception.ConfiguredLimitBytes);
            Assert.False(secondReplayAdmission.HasCompleteAdmissionFor(source.Length));
        }
        Assert.Equal(1, locator.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "durable-copy-replay-respects-current-body-limit")]
    public void DurableCopiedEnvelope_RejectsAProvenBodyUnderATighterCurrentLimit()
    {
        byte[] source = "<ABC>"u8.ToArray();
        var originalAdmission = Admission(3, 5);
        var locator = new TestCopiedLocator { LocatedRange = (true, 1, 3) };
        _ = AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
            Serialization(locator), originalAdmission, durableProof: null);
        Assert.True(originalAdmission.TryCreateDurableProof(BinaryContentType.ToString(), out DurablePayloadAdmissionProof proof));

        var replayAdmission = Admission(2, 5);
        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() =>
            AdmittedCopyMessageBody.Create(source.AsMemory(), BinaryContentType,
                Serialization(locator), replayAdmission, proof));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
        Assert.Equal(3, exception.ActualBytes);
        Assert.Equal(2, exception.ConfiguredLimitBytes);
        Assert.Equal(1, locator.Calls);
        Assert.False(replayAdmission.HasCompleteAdmissionFor(source.Length));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.example.event+json")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "send-only-json-charges-entire-envelope-and-preserves-text")]
    public void CopiedSendOnlyJson_ChargesTheWholeEnvelopeAndExposesLosslessText(string mediaType)
    {
        byte[] source = "{\"value\":7}"u8.ToArray();
        var contentType = new ContentType(mediaType);
        var locator = new TestCopiedLocator { LocatedRange = (true, 0, 1) };
        var admission = Admission(source.Length, source.Length);

        AdmittedCopyMessageBody body = AdmittedCopyMessageBody.Create(
            source.AsMemory(), contentType, Serialization(locator), admission, durableProof: null);

        Assert.Equal(0, locator.Calls);
        Assert.Equal(source.Length, body.Length);
        Assert.True(admission.HasCompleteAdmissionFor(source.Length));
        Assert.True(admission.TryCreateDurableProof(contentType.ToString(), out DurablePayloadAdmissionProof proof));
        Assert.Equal(source.Length, proof.SerializedBodyBytes);
        Assert.True(body.TryGetTransportText(out string? text));
        Assert.Equal("{\"value\":7}", text);
        Assert.Equal(source, Encoding.UTF8.GetBytes(text));

        source[1] = (byte)'X';
        Assert.Equal("{\"value\":7}"u8.ToArray(), body.ToArray());
        Assert.True(body.TryGetTransportText(out string? retainedText));
        Assert.Equal("{\"value\":7}", retainedText);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "send-only-json-rejects-whole-envelope-at-body-limit")]
    public void CopiedSendOnlyJson_RejectsTheWholeEnvelopeAtTheBodyLimit()
    {
        byte[] source = "{\"value\":7}"u8.ToArray();
        var locator = new TestCopiedLocator { LocatedRange = (true, 0, 1) };
        var admission = Admission(source.Length - 1, source.Length);

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() =>
            AdmittedCopyMessageBody.Create(source.AsMemory(), new ContentType("application/json"),
                Serialization(locator), admission, durableProof: null));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
        Assert.Equal(source.Length, exception.ActualBytes);
        Assert.Equal(source.Length - 1, exception.ConfiguredLimitBytes);
        Assert.Equal(0, locator.Calls);
        Assert.False(admission.HasCompleteAdmissionFor(source.Length));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.example.event+json")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "send-only-invalid-json-is-retained-without-text")]
    public void CopiedSendOnlyJson_KeepsInvalidBytesWithoutOfferingJsonText(string mediaType)
    {
        byte[][] invalidEnvelopes = ["{\"value\":"u8.ToArray(), [0xff]];
        var contentType = new ContentType(mediaType);
        var locator = new TestCopiedLocator { LocatedRange = (true, 0, 1) };

        foreach (byte[] source in invalidEnvelopes)
        {
            var admission = Admission(source.Length, source.Length);

            AdmittedCopyMessageBody body = AdmittedCopyMessageBody.Create(
                source.AsMemory(), contentType, Serialization(locator), admission, durableProof: null);

            Assert.Equal(0, locator.Calls);
            Assert.True(admission.HasCompleteAdmissionFor(source.Length));
            Assert.True(admission.TryCreateDurableProof(contentType.ToString(), out DurablePayloadAdmissionProof proof));
            Assert.Equal(source.Length, proof.SerializedBodyBytes);
            Assert.Equal(source, body.ToArray());
            Assert.False(body.TryGetTransportText(out string? text));
            Assert.Null(text);
        }
    }

    private static BoundedSerializerMessageBody Create(
        TestBoundedSerializer serializer, PayloadAdmissionSerializationContext admission)
        => BoundedSerializerMessageBody.Create(new MessageSendContext<TestMessage>(new TestMessage()), serializer, admission);

    private static PayloadAdmissionSerializationContext Admission(int maximumBodyBytes, int maximumEnvelopeBytes,
        bool messageDataOffloadObserved = false, int? messageDataThresholdBytes = null)
    {
        var policy = new PayloadAdmissionPolicy
        {
            MaximumSerializedBodyBytes = maximumBodyBytes,
            MaximumTransportEnvelopeBytes = maximumEnvelopeBytes,
            MessageDataOffloadThresholdBytes = messageDataThresholdBytes,
        };
        return new PayloadAdmissionSerializationContext(
            new PayloadAdmissionRuntime<IBus>(new PayloadAdmissionEvaluator<IBus>(policy)), messageDataOffloadObserved);
    }

    private static SerializerCollection Serialization(TestCopiedLocator locator)
    {
        var serializer = new TestBoundedSerializer();
        return new SerializerCollection([serializer], BinaryContentType, [locator], BinaryContentType);
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private sealed class TestMessage;

    private sealed class TestBoundedSerializer : IBoundedMessageSerializer
    {
        public byte[] BodyBytes { get; init; } = [];
        public byte[] Prefix { get; init; } = "<"u8.ToArray();
        public byte[] Suffix { get; init; } = ">"u8.ToArray();
        public byte[]? EnvelopeBodyOverride { get; init; }
        public int BodyReservationHint { get; init; }
        public (bool Found, int Offset, int Length)? LocatedRange { get; init; }
        public SerializedTransportTextFormat TransportTextFormat { get; init; }
        public ContentType ContentType => BinaryContentType;
        public int BodyWrites { get; private set; }
        public int EnvelopeWrites { get; private set; }
        public int LocatorCalls { get; private set; }
        public bool BodyStreamWasWritable { get; private set; }
        public long BodyStreamInitialPosition { get; private set; }
        public byte[]? ObservedSerializedBody { get; private set; }
        public byte[]? ObservedEnvelope { get; private set; }
        public Memory<byte> RetainedBodyMemory { get; private set; }
        public Memory<byte> RetainedEnvelopeMemory { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
            => throw new NotSupportedException("The bounded admission entry point must not call the legacy serializer method.");

        public void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer) where T : class
        {
            BodyWrites++;
            if (BodyReservationHint > 0)
                writer.GetMemory(BodyReservationHint);
            if (BodyBytes.Length == 0)
                return;
            RetainedBodyMemory = writer.GetMemory(BodyBytes.Length);
            BodyBytes.AsSpan().CopyTo(RetainedBodyMemory.Span);
            writer.Advance(BodyBytes.Length);
        }

        public void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
            where T : class
        {
            EnvelopeWrites++;
            BodyStreamWasWritable = serializedBody.CanWrite;
            BodyStreamInitialPosition = serializedBody.Position;
            ObservedSerializedBody = ReadAll(serializedBody);
            byte[] body = EnvelopeBodyOverride ?? ObservedSerializedBody;
            byte[] envelope = [.. Prefix, .. body, .. Suffix];
            RetainedEnvelopeMemory = writer.GetMemory(envelope.Length);
            envelope.AsSpan().CopyTo(RetainedEnvelopeMemory.Span);
            writer.Advance(envelope.Length);
        }

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length)
        {
            LocatorCalls++;
            ObservedEnvelope = serializedEnvelope.ToArray();
            (bool found, offset, length) = LocatedRange ?? (true, Prefix.Length, BodyBytes.Length);
            return found;
        }
    }

    private sealed class TestCopiedLocator : IMessageDeserializer, ICopiedEnvelopeBodyLocator
    {
        public (bool Found, int Offset, int Length) LocatedRange { get; init; }
        public int Calls { get; private set; }
        public byte[]? ObservedEnvelope { get; private set; }
        public ContentType ContentType => BinaryContentType;

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> envelope, out int offset, out int length)
        {
            Calls++;
            ObservedEnvelope = envelope.ToArray();
            (bool found, offset, length) = LocatedRange;
            return found;
        }

        public ConsumeContext Deserialize(ReceiveContext receiveContext) => throw new NotSupportedException();
        public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
            => throw new NotSupportedException();
        public MessageBody GetMessageBody(string text) => throw new NotSupportedException();
        public void Probe(ProbeContext context) => throw new NotSupportedException();
    }
}
