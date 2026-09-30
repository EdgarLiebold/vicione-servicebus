using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonForwardingSerializerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "serialized-forwarding-headers-remain-bound")]
    public void ForwardedBody_RejectsLaterApplicationHeaderChangesAndPreservesInheritedHeaders()
    {
        var envelope = new JsonMessageEnvelope
        {
            Message = new { value = "preserved" },
            Headers = new Dictionary<string, object?> { ["inherited"] = "source-value", ["optional"] = null },
        };
        var forwarding = new SystemTextJsonForwardingSerializer(envelope,
            SystemTextJsonMessageSerializer.JsonContentType, ServiceBusMetadataJson.Options);
        var context = new MessageSendContext<object>(new object()) { Serializer = forwarding };
        context.Headers.Set("application", "before");

        using JsonDocument encoded = JsonDocument.Parse(context.Body.ToArray());
        JsonElement headers = encoded.RootElement.GetProperty("headers");
        Assert.Equal("source-value", headers.GetProperty("inherited").GetString());
        Assert.Equal(JsonValueKind.Null, headers.GetProperty("optional").ValueKind);
        Assert.Equal("before", headers.GetProperty("application").GetString());
        MessageJournalCapture capture = MessageJournalCaptureFactory.CreateSend(
            context, MessageJournalOperation.Send, MessageJournalOutcome.Succeeded, null);
        Assert.Equal("source-value", capture.Headers["inherited"]);
        Assert.Equal("before", capture.Headers["application"]);

        context.Headers.Set("application", "after");
        MessageException failure = Assert.Throws<MessageException>(() => context.Body.ToArray());
        Assert.Contains("application", failure.Message, StringComparison.Ordinal);
        Assert.Equal("before", context.Headers.Get<string>("application"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "late-diagnostic-and-scheduling-headers-preserve-serialized-body")]
    public void SerializedBody_AllowsLateDiagnosticAndSchedulingTransportHeaders()
    {
        var context = new MessageSendContext<object>(new { value = "payload" })
        {
            Serializer = ServiceBusMetadataJson.MessageSerializer,
        };
        context.Headers.Set("application", "before");
        byte[] serialized = context.Body.ToArray();

        context.Headers.Set("VSB-Activity-Id", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
        context.Headers.Set(MessageHeaders.SchedulingTokenId, "token");

        Assert.Equal(serialized, context.Body.ToArray());
        Assert.Equal("before", context.Headers.Get<string>("application"));
        Assert.Equal("token", context.Headers.Get<string>(MessageHeaders.SchedulingTokenId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "custom-header-converter-survives-body-and-journal-binding")]
    public void SerializedBody_UsesTheConfiguredHeaderConverterForMutationGuardsAndJournal()
    {
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(new ConverterHeaderJsonConverter());
        options.MakeReadOnly();
        var header = new ConverterHeader { Kind = typeof(string) };
        var context = new MessageSendContext<object>(new { value = "payload" })
        {
            Serializer = new SystemTextJsonMessageSerializer(options),
        };
        context.Headers.Set("converter-header", header);

        using JsonDocument encoded = JsonDocument.Parse(context.Body.ToArray());
        Assert.Equal(typeof(string).AssemblyQualifiedName,
            encoded.RootElement.GetProperty("headers").GetProperty("converter-header").GetString());
        MessageJournalCapture capture = MessageJournalCaptureFactory.CreateSend(
            context, MessageJournalOperation.Send, MessageJournalOutcome.Succeeded, null);
        Assert.Equal(header.ToString(), capture.Headers["converter-header"]);

        header.Kind = typeof(int);
        MessageException failure = Assert.Throws<MessageException>(() => context.Body.ToArray());
        Assert.Contains("converter-header", failure.Message, StringComparison.Ordinal);
        Assert.Equal(typeof(string), Assert.IsType<ConverterHeader>(
            context.Headers.Get<ConverterHeader>("converter-header")).Kind);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "converter-side-effect-cannot-diverge-envelope-and-context")]
    public void SerializedBody_RejectsAHeaderConverterThatMutatesAfterWritingWireValue()
    {
        var converter = new ConverterHeaderJsonConverter(mutateOnWrite: 3);
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(converter);
        options.MakeReadOnly();
        var context = new MessageSendContext<object>(new { value = "payload" })
        {
            Serializer = new SystemTextJsonMessageSerializer(options),
        };
        context.Headers.Set("converter-header", new ConverterHeader { Kind = typeof(string) });

        MessageException failure = Assert.Throws<MessageException>(() => context.Body.ToArray());

        Assert.Contains("converter-header", failure.Message, StringComparison.Ordinal);
        Assert.True(converter.WriteCalls >= 3);
        Assert.Equal(typeof(string), Assert.IsType<ConverterHeader>(
            context.Headers.Get<ConverterHeader>("converter-header")).Kind);
        Assert.Throws<InvalidOperationException>(() => MessageJournalCaptureFactory.CreateSend(
            context, MessageJournalOperation.Send, MessageJournalOutcome.Faulted, failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "write-only-header-converter-restores-replaced-value")]
    public void SerializedBody_RestoresReplacedHeaderWhenTheConverterCannotRead()
    {
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(new ConverterHeaderJsonConverter(canRead: false));
        options.MakeReadOnly();
        var original = new ConverterHeader { Kind = typeof(string) };
        var context = new MessageSendContext<object>(new { value = "payload" })
        {
            Serializer = new SystemTextJsonMessageSerializer(options),
        };
        context.Headers.Set("converter-header", original);
        _ = context.Body.ToArray();

        context.Headers.Set("converter-header", new ConverterHeader { Kind = typeof(int) });
        MessageException failure = Assert.Throws<MessageException>(() => context.Body.ToArray());

        Assert.Contains("converter-header", failure.Message, StringComparison.Ordinal);
        Assert.Same(original, context.Headers.Get<ConverterHeader>("converter-header"));
        Assert.Throws<InvalidOperationException>(() => MessageJournalCaptureFactory.CreateSend(
            context, MessageJournalOperation.Send, MessageJournalOutcome.Faulted, failure));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "journal-header-formatting-cannot-change-bound-identity")]
    public void JournalCapture_RejectsHeaderTextConversionThatChangesMessageIdentity()
    {
        Guid originalId = Guid.NewGuid();
        Guid changedId = Guid.NewGuid();
        var context = new MessageSendContext<object>(new { value = "payload" })
        {
            MessageId = originalId,
            Serializer = ServiceBusMetadataJson.MessageSerializer,
        };
        context.Headers.Set("formatted-header", new HeaderToStringMutator(() => context.MessageId = changedId));
        _ = context.Body.ToArray();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            MessageJournalCaptureFactory.CreateSend(
                context, MessageJournalOperation.Send, MessageJournalOutcome.Succeeded, null));

        Assert.Contains("metadata changed", failure.Message, StringComparison.Ordinal);
        Assert.Equal(originalId, context.MessageId);
    }

    private sealed class HeaderToStringMutator(Action mutate)
    {
        public override string ToString()
        {
            mutate();
            return "formatted";
        }
    }

    private sealed class ConverterHeader
    {
        public required Type Kind { get; set; }
    }

    private sealed class ConverterHeaderJsonConverter(int mutateOnWrite = int.MaxValue, bool canRead = true)
        : JsonConverter<ConverterHeader>
    {
        public int WriteCalls { get; private set; }

        public override ConverterHeader? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!canRead)
                throw new NotSupportedException("This header converter only writes values.");
            return new ConverterHeader { Kind = Type.GetType(reader.GetString()!, throwOnError: true)! };
        }

        public override void Write(Utf8JsonWriter writer, ConverterHeader value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Kind.AssemblyQualifiedName);
            if (++WriteCalls == mutateOnWrite)
                value.Kind = typeof(int);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "recursive-object-and-array-overlay")]
    public void ObjectOverlay_RecursivelyMergesObjectsAndAppendsArrayElements()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new
        {
            values = new[] { 1 },
            nested = new { replaced = 2, preserved = 3 },
            preserved = "original",
        });
        var serializer = CreateRawSerializer(original);
        serializer.Overlay(new
        {
            values = new[] { 4, 5 },
            nested = new { replaced = 7 },
            preserved = (string?)null,
            added = true,
        });

        JsonElement result = Serialize(serializer);

        Assert.Equal([1, 4, 5], result.GetProperty("values").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(7, result.GetProperty("nested").GetProperty("replaced").GetInt32());
        Assert.Equal(3, result.GetProperty("nested").GetProperty("preserved").GetInt32());
        Assert.Equal("original", result.GetProperty("preserved").GetString());
        Assert.True(result.GetProperty("added").GetBoolean());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "top-level-array-overlay")]
    public void ArrayOverlay_AppendsReplacementElementsInOrder()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new[] { 1, 2 });
        var serializer = CreateRawSerializer(original);

        serializer.Overlay(new[] { 3, 4 });

        Assert.Equal([1, 2, 3, 4], Serialize(serializer).EnumerateArray().Select(x => x.GetInt32()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "incompatible-shape-replacement")]
    public void IncompatibleOverlayShape_ReplacesTheOriginalJsonValue()
    {
        JsonElement original = JsonSerializer.SerializeToElement(new[] { 1, 2 });
        var serializer = CreateRawSerializer(original);

        serializer.Overlay(new { value = 73 });

        Assert.Equal(73, Serialize(serializer).GetProperty("value").GetInt32());
    }

    private static SystemTextJsonForwardingSerializer CreateRawSerializer(JsonElement message) =>
        new(message, SystemTextJsonRawMessageSerializer.JsonContentType, ServiceBusMetadataJson.Options, RawSerializerOptions.None);

    private static JsonElement Serialize(SystemTextJsonForwardingSerializer serializer)
    {
        var context = new MessageSendContext<object>(new object());
        return JsonSerializer.Deserialize<JsonElement>(serializer.GetMessageBody(context).ToArray(), ServiceBusMetadataJson.Options);
    }
}
