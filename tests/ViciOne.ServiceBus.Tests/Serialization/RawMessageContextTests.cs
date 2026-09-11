using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class RawMessageContextTests
{
    private static readonly Guid MessageId = Guid.Parse("d3bf4033-c220-4a34-9997-9e9c5941a6d1");

    [Fact]
    [RequirementCoverage("REQ-VSB-RAW-HEADERS", "copy-policy-applies-to-every-access-path")]
    public void CopyHeaders_ExposesOnlyApplicationHeadersThroughEveryAccessPath()
    {
        var transportHeaders = new DictionarySendHeaders();
        transportHeaders.Set(MessageHeaders.MessageId, MessageId.ToString("D"));
        transportHeaders.Set(MessageHeaders.Host.Info, "{}");
        transportHeaders.Set(DiagnosticHeaders.ActivityId, "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
        transportHeaders.Set("application-header", "visible");
        var context = new RawMessageContext(transportHeaders, new Uri("loopback://input"), RawSerializerOptions.CopyHeaders);

        Assert.Equal(MessageId, context.MessageId);
        Assert.True(context.Headers.TryGetHeader("application-header", out object? value));
        Assert.Equal("visible", value);
        Assert.Equal("visible", context.Headers.Get<string>("application-header"));
        Assert.False(context.Headers.TryGetHeader(MessageHeaders.MessageId, out _));
        Assert.Null(context.Headers.Get<string>(MessageHeaders.MessageId));
        Assert.False(context.Headers.TryGetHeader(MessageHeaders.Host.Info.ToLowerInvariant(), out _));
        Assert.True(context.Headers.TryGetHeader(DiagnosticHeaders.ActivityId.ToLowerInvariant(), out object? activityId));
        Assert.Equal("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01", activityId);
        Assert.Equal(2, context.Headers.GetAll().Count());
        Assert.Equal(2, context.Headers.Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RAW-HEADERS", "disabled-copy-policy-applies-to-every-access-path")]
    public void DisabledCopyHeaders_HidesApplicationHeadersThroughEveryAccessPath()
    {
        var transportHeaders = new DictionarySendHeaders();
        transportHeaders.Set("application-header", "hidden");
        var context = new RawMessageContext(transportHeaders, destinationAddress: null, RawSerializerOptions.None);

        Assert.False(context.Headers.TryGetHeader("application-header", out _));
        Assert.Equal("fallback", context.Headers.Get("application-header", "fallback"));
        Assert.Empty(context.Headers.GetAll());
        Assert.Empty(context.Headers);
        Assert.NotNull(context.Host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RAW-FORWARDING", "replacement-message-types-preserved")]
    public void ForwardSerializer_PreservesExplicitReplacementMessageTypes()
    {
        var serializer = new SystemTextJsonRawMessageSerializer(
            ServiceBusMetadataJson.Options,
            RawSerializerOptions.None);
        SerializerContext received = serializer.Deserialize(
            new StringMessageBody("{\"value\":1}"),
            new DictionarySendHeaders());
        string[] messageTypes = [MessageUrn.ForTypeString<ReplacementMessage>()];
        IMessageSerializer forwardSerializer = received.GetMessageSerializer(
            new ReplacementMessage(27),
            messageTypes);
        var sendContext = new MessageSendContext<ReplacementMessage>(new ReplacementMessage(27));

        _ = forwardSerializer.GetMessageBody(sendContext);

        Assert.Equal(messageTypes, sendContext.SupportedMessageTypes);
        Assert.NotSame(messageTypes, sendContext.SupportedMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RAW-SERIALIZER", "unsupported-option-bits-rejected")]
    public void Constructors_RejectUnsupportedRawSerializerOptionBits()
    {
        var invalid = (RawSerializerOptions)8;

        ArgumentOutOfRangeException serializer = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options, invalid));
        ArgumentOutOfRangeException context = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RawMessageContext(new DictionarySendHeaders(), null, invalid));

        Assert.Equal("rawOptions", serializer.ParamName);
        Assert.Equal("options", context.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RAW-SERIALIZER", "text-body-and-probe-contract")]
    public void TextBodyAndProbe_ExposeTheExactRawJsonContract()
    {
        var serializer = new SystemTextJsonRawMessageSerializer(
            ServiceBusMetadataJson.Options,
            RawSerializerOptions.None);

        MessageBody body = serializer.GetMessageBody("{\"value\":27}");
        IProbeResult probe = serializer.GetProbeResult(TestContext.Current.CancellationToken);
        string probeJson = JsonSerializer.Serialize(probe.Results);

        Assert.Equal("{\"value\":27}", body.GetString());
        Assert.Contains(SystemTextJsonRawMessageSerializer.JsonMediaType, probeJson, StringComparison.Ordinal);
        Assert.Contains("System.Text.Json", probeJson, StringComparison.Ordinal);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => serializer.GetMessageBody(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => serializer.Probe(null!)).ParamName);
    }

    private sealed record ReplacementMessage(int Value);
}
