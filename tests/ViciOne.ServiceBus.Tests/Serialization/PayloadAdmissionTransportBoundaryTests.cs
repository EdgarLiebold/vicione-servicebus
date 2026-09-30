using System.Buffers;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class PayloadAdmissionTransportBoundaryTests
{
    [Fact]
    public void BodySerializedBeforeAdmission_IsRejectedWithoutAttachingAProof()
    {
        var context = CreateContext();
        Assert.Equal(2, context.Body.Length);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => PayloadAdmissionTransportBoundary.Admit(CreateRuntime(), context));

        Assert.Contains("serialized before payload admission", exception.Message, StringComparison.Ordinal);
        Assert.False(context.TryGetPayload(out PayloadAdmissionSerializationContext? _));
    }

    [Fact]
    public void AdmissionOwnedByAnotherBus_CannotBeReusedForThisSend()
    {
        var originalRuntime = CreateRuntime();
        var otherRuntime = CreateRuntime();
        var context = CreateContext();
        PayloadAdmissionSerializationContext original = context.GetOrAddPayload(
            () => new PayloadAdmissionSerializationContext(originalRuntime, messageDataOffloadObserved: false));

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => PayloadAdmissionTransportBoundary.Admit(otherRuntime, context));

        Assert.Contains("different bus", exception.Message, StringComparison.Ordinal);
        Assert.Same(originalRuntime, original.OwnerRuntime);
        Assert.True(context.TryGetPayload(out PayloadAdmissionSerializationContext? retained));
        Assert.Same(original, retained);
        Assert.Null(context.BodyLength);
    }

    [Fact]
    public void MessageOnlySendView_CannotCrossThePhysicalTransportBoundary()
    {
        var transport = CreateContext();
        var proxy = new SendContextProxy<PayloadMessage>(transport, transport.Message);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => PayloadAdmissionTransportBoundary.Admit(CreateRuntime(), proxy));

        Assert.Contains("requires a transport send context", exception.Message, StringComparison.Ordinal);
        Assert.Null(transport.BodyLength);
        Assert.False(transport.TryGetPayload(out PayloadAdmissionSerializationContext? _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("application/octet-stream")]
    public void ContentTypeChangedAfterSerializerSelection_IsRejectedBeforeSerialization(string? contentType)
    {
        var context = CreateContext();
        context.ContentType = contentType is null ? null : new ContentType(contentType);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => PayloadAdmissionTransportBoundary.Admit(CreateRuntime(), context));

        Assert.Contains("content type no longer matches its serializer", exception.Message, StringComparison.Ordinal);
        Assert.Null(context.BodyLength);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION", "serializer-getter-cannot-change-physical-destination")]
    public void SerializerGetterChangingDestination_IsRejectedBeforePhysicalAdmission()
    {
        var context = new MessageSendContext<PayloadMessage>(new PayloadMessage("send"))
        {
            MessageId = Guid.NewGuid(),
            DestinationAddress = new Uri("loopback://admission/original"),
        };
        Uri originalDestination = context.DestinationAddress!;
        var serializer = new DestinationChangingSerializer(() =>
            context.DestinationAddress = new Uri("loopback://admission/replacement"));
        context.Serializer = serializer;
        serializer.Arm();

        MessageException failure = Assert.Throws<MessageException>(
            () => PayloadAdmissionTransportBoundary.Admit(CreateRuntime(), context));

        Assert.Contains("DestinationAddress", failure.Message, StringComparison.Ordinal);
        Assert.True(serializer.GetterCallsAfterArming > 0);
        Assert.Equal(originalDestination, context.DestinationAddress);
    }

    static MessageSendContext<PayloadMessage> CreateContext()
    {
        var context = new MessageSendContext<PayloadMessage>(new PayloadMessage("send"));
        context.Serializer = new CopyBodySerializer(
            new ContentType("application/json"), new BinaryMessageBody("{}"u8.ToArray()));
        return context;
    }

    static PayloadAdmissionRuntime<IBus> CreateRuntime()
    {
        var policy = new PayloadAdmissionPolicy
        {
            MaximumSerializedBodyBytes = 1024,
            MaximumTransportEnvelopeBytes = 1024,
        };
        return new PayloadAdmissionRuntime<IBus>(new PayloadAdmissionEvaluator<IBus>(policy));
    }

    private sealed class DestinationChangingSerializer(Action changeDestination) : IBoundedMessageSerializer
    {
        private readonly ContentType _contentType = new("application/octet-stream");
        private bool _armed;

        public int GetterCallsAfterArming { get; private set; }

        public ContentType ContentType
        {
            get
            {
                if (_armed)
                {
                    GetterCallsAfterArming++;
                    changeDestination();
                }
                return _contentType;
            }
        }

        public SerializedTransportTextFormat TransportTextFormat => SerializedTransportTextFormat.Base64;

        public void Arm() => _armed = true;

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            throw new NotSupportedException("The bounded serializer path must be used.");

        public void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer) where T : class
        {
            writer.GetSpan(1)[0] = 42;
            writer.Advance(1);
        }

        public void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
            where T : class
        {
            int value = serializedBody.ReadByte();
            if (value < 0)
                throw new InvalidOperationException("The admitted application body was empty.");
            writer.GetSpan(1)[0] = (byte)value;
            writer.Advance(1);
        }

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length)
        {
            offset = 0;
            length = serializedEnvelope.Length;
            return length == 1;
        }
    }

    sealed record PayloadMessage(string Value);
}
