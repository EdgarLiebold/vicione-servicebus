using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageJournal;

public sealed class MessageJournalCaptureFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "pre-capture-correlation-drift-is-restored")]
    public void SerializedCorrelationDrift_IsRejectedAndRestoredBeforeJournalCapture()
    {
        var context = CreateContext();
        Guid original = Guid.NewGuid();
        context.CorrelationId = original;
        Assert.Equal([42], context.Body.ToArray());
        context.CorrelationId = Guid.NewGuid();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => Capture(context));

        Assert.Contains("metadata changed before journal capture", failure.Message, StringComparison.Ordinal);
        Assert.Equal(original, context.CorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "pre-capture-contract-drift-is-restored")]
    public void SerializedContractDrift_IsRejectedAndRestoredBeforeJournalCapture()
    {
        var context = CreateContext();
        string[] original = context.SupportedMessageTypes.ToArray();
        Assert.Equal([42], context.Body.ToArray());
        context.SupportedMessageTypes = ["urn:wrong:contract"];

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => Capture(context));

        Assert.Contains("message types changed before journal capture", failure.Message, StringComparison.Ordinal);
        Assert.Equal(original, context.SupportedMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "pre-capture-native-route-drift-is-restored")]
    public void SerializedNativeRouteDrift_IsRejectedAndRestoredBeforeJournalCapture()
    {
        var context = new NativeSendContext { Route = "orders.original", Serializer = Serializer() };
        Assert.Equal([42], context.Body.ToArray());
        context.Route = "orders.replacement";

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => Capture(context));

        Assert.Contains("native metadata changed before journal capture", failure.Message, StringComparison.Ordinal);
        Assert.Equal("orders.original", context.Route);
    }

    private static MessageSendContext<ProbeMessage> CreateContext() => new(new ProbeMessage())
    {
        Serializer = Serializer(),
    };

    private static CopyBodySerializer Serializer() =>
        new(new ContentType("application/json"), new BinaryMessageBody(new byte[] { 42 }));

    private static MessageJournalCapture Capture(MessageSendContext<ProbeMessage> context) =>
        MessageJournalCaptureFactory.CreateSend(
            context, MessageJournalOperation.Send, MessageJournalOutcome.Succeeded, exception: null);

    private sealed class NativeSendContext : MessageSendContext<ProbeMessage>, ITransportSendMetadata
    {
        public NativeSendContext() : base(new ProbeMessage()) { }

        public string Route { get; set; } = "orders.original";

        public object CaptureNativeMetadata() => Route;

        public string? ChangedNativeField(object snapshot) =>
            string.Equals(Route, (string)snapshot, StringComparison.Ordinal) ? null : nameof(Route);

        public void RestoreNativeMetadata(object snapshot) => Route = (string)snapshot;
    }

    private sealed record ProbeMessage;
}
