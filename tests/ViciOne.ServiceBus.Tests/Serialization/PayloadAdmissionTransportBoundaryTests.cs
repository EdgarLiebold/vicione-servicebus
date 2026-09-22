using System.Net.Mime;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;
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

    sealed record PayloadMessage(string Value);
}
