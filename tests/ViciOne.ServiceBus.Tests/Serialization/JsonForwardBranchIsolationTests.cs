using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class JsonForwardBranchIsolationTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-FORWARDING", "two-target-json-body-and-metadata-isolation")]
    public async Task ForwardedInterface_PreservesUnknownPayloadAndKeepsTargetMetadataIndependentAsync(bool raw)
    {
        using var harness = new InMemoryTestHarness($"json-forward-branches-{NewId.NextGuid():N}") { TestTimeout = Timeout };
        harness.BeginTestScope();
        var deliveries = new ConcurrentQueue<ConsumeContext<CompleteMessage>>();
        var admitted = new ForwardAdmissionObserver();
        var first = new TaskCompletionSource<ConsumeContext<CompleteMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<ConsumeContext<CompleteMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new TaskCompletionSource<(Guid? Correlation, string? Branch, string Payload)>(TaskCreationOptions.RunContinuationsAsynchronously);
        Uri firstAddress = new(harness.BaseAddress, "first");
        Uri secondAddress = new(harness.BaseAddress, "second");
        Guid originalCorrelation = NewId.NextGuid();
        Guid firstCorrelation = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        var message = new CompleteMessage
        {
            Id = NewId.NextGuid(),
            Extra = JsonSerializer.Deserialize<JsonElement>("""{"nested":[{"value":17},null,["tail",false]],"keep":"original"}"""),
        };
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.ConnectSendObserver(admitted);
            if (raw)
                bus.UseRawJsonSerializer(RawSerializerOptions.Default, true);
            bus.ReceiveEndpoint("first", endpoint => endpoint.Handler<CompleteMessage>(context =>
            {
                deliveries.Enqueue(context);
                first.TrySetResult(context);
                return Task.CompletedTask;
            }));
            bus.ReceiveEndpoint("second", endpoint => endpoint.Handler<CompleteMessage>(context =>
            {
                deliveries.Enqueue(context);
                second.TrySetResult(context);
                return Task.CompletedTask;
            }));
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Handler<SelectedMessage>(async context =>
        {
            await context.ForwardAsync(firstAddress, Pipe.Execute<SendContext<SelectedMessage>>(outgoing =>
            {
                outgoing.CorrelationId = firstCorrelation;
                outgoing.Headers.Set("branch", "first");
                outgoing.Headers.Set("first-only", "private");
            }));
            await context.ForwardAsync(secondAddress, Pipe.Execute<SendContext<SelectedMessage>>(outgoing =>
                outgoing.Headers.Set("branch", "second")));
            source.TrySetResult((context.CorrelationId, context.Headers.Get<string>("branch"),
                Encoding.UTF8.GetString(context.Advanced().ReceiveContext.Body.ToArray())));
        });
        try
        {
            await harness.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(message, context =>
            {
                context.MessageId = messageId;
                context.CorrelationId = originalCorrelation;
                context.Headers.Set("shared", "source");
            }, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            var original = await source.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            string[] originalTypes = Assert.Single(admitted.Messages, entry => entry.Address == harness.InputQueueAddress).Types;
            foreach (Uri target in new[] { firstAddress, secondAddress })
            {
                string[] types = Assert.Single(admitted.Messages, entry => entry.Address == target).Types;
                Assert.Equal(originalTypes, types);
                Assert.Contains(MessageUrn.ForTypeString<CompleteMessage>(), types);
                Assert.Contains(MessageUrn.ForTypeString<SelectedMessage>(), types);
            }
            ConsumeContext<CompleteMessage> firstDelivery = await first.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            ConsumeContext<CompleteMessage> secondDelivery = await second.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal(firstCorrelation, firstDelivery.CorrelationId);
            Assert.Equal(originalCorrelation, secondDelivery.CorrelationId);
            Assert.Equal(originalCorrelation, original.Correlation);
            Assert.Null(original.Branch);
            Assert.Equal("first", firstDelivery.Headers.Get<string>("branch"));
            Assert.Equal("second", secondDelivery.Headers.Get<string>("branch"));
            Assert.Equal("private", firstDelivery.Headers.Get<string>("first-only"));
            Assert.False(secondDelivery.Headers.TryGetHeader("first-only", out _));
            foreach (ConsumeContext<CompleteMessage> delivery in new[] { firstDelivery, secondDelivery })
            {
                Assert.Equal(messageId, delivery.MessageId);
                Assert.Equal(message.Id, delivery.Message.Id);
                Assert.True(JsonElement.DeepEquals(message.Extra, delivery.Message.Extra));
                Assert.Equal("source", delivery.Headers.Get<string>("shared"));
                Headers forwardingHeaders = raw ? delivery.Advanced().ReceiveContext.TransportHeaders : delivery.Headers;
                Assert.Equal(harness.InputQueueAddress.ToString(), forwardingHeaders.Get<string>(MessageHeaders.ForwarderAddress));
                if (raw)
                    Assert.False(delivery.Headers.TryGetHeader(MessageHeaders.ForwarderAddress, out _));
            }
            using JsonDocument originalDocument = JsonDocument.Parse(original.Payload);
            JsonElement originalPayload = raw ? originalDocument.RootElement : originalDocument.RootElement.GetProperty("message");
            Assert.True(JsonElement.DeepEquals(message.Extra, originalPayload.GetProperty("extra")));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Equal(2, deliveries.Count);
        Assert.Equal(1, deliveries.Count(delivery => delivery.DestinationAddress == firstAddress));
        Assert.Equal(1, deliveries.Count(delivery => delivery.DestinationAddress == secondAddress));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-JSON-FORWARDING", "context-overlay-isolates-nested-sibling-payloads")]
    public void ForwardedReplacement_PreservesNestedUnknownValuesAndIsolatesSiblingMessages(bool raw)
    {
        const string Original = """{"name":"source","nested":{"keep":3,"replace":4},"items":[{"values":[1,null]}],"untouched":[[false],null]}""";
        Guid originalId = NewId.NextGuid();
        var envelope = new JsonMessageEnvelope
        {
            MessageId = originalId.ToString("D"),
            MessageTypes = [MessageUrn.ForTypeString<CompleteMessage>()],
            Message = JsonSerializer.Deserialize<JsonElement>(Original),
            Headers = new Dictionary<string, object?> { ["shared"] = "source" },
        };
        IMessageDeserializer deserializer = raw
            ? new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options, RawSerializerOptions.Default)
            : new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);
        string originalWire = raw ? Original : JsonSerializer.Serialize(envelope, ServiceBusMetadataJson.Options);
        SerializerContext context = deserializer.Deserialize(new StringMessageBody(originalWire), EmptyHeaders.Instance);
        IMessageSerializer first = context.GetMessageSerializer(envelope, new
        {
            name = "first",
            nested = new { replace = 9 },
            items = new[] { new { values = new[] { 2 } } },
            added = (string?)null,
        });
        IMessageSerializer second = context.GetMessageSerializer(envelope, new
        {
            name = "second",
            nested = new { replace = 7 },
            items = new[] { new { values = new[] { 5 } } },
        });
        JsonElement firstPayload = Serialize(first, raw, "first");
        JsonElement secondPayload = Serialize(second, raw, "second");

        AssertPayload(firstPayload, "first", 9, 2);
        AssertPayload(secondPayload, "second", 7, 5);
        Assert.Equal(JsonValueKind.Null, firstPayload.GetProperty("added").ValueKind);
        Assert.False(secondPayload.TryGetProperty("added", out _));
        Assert.Equal(Original, Assert.IsType<JsonElement>(envelope.Message).GetRawText());
        Assert.Equal(originalId.ToString("D"), envelope.MessageId);
        Assert.Equal("source", Assert.Single(envelope.Headers).Value);
        AssertPayload(Serialize(first, raw, "first-again"), "first", 9, 2);
    }

    private static JsonElement Serialize(IMessageSerializer serializer, bool raw, string branch)
    {
        var send = new MessageSendContext<object>(new object()) { MessageId = NewId.NextGuid() };
        send.Headers.Set("branch", branch);
        using JsonDocument document = JsonDocument.Parse(serializer.GetMessageBody(send).ToArray());
        if (!raw)
        {
            Assert.Equal(branch, document.RootElement.GetProperty("headers").GetProperty("branch").GetString());
            Assert.Equal("source", document.RootElement.GetProperty("headers").GetProperty("shared").GetString());
            Assert.Equal(send.MessageId!.Value, document.RootElement.GetProperty("messageId").GetGuid());
        }
        return (raw ? document.RootElement : document.RootElement.GetProperty("message")).Clone();
    }

    private static void AssertPayload(JsonElement payload, string name, int replaced, int appended)
    {
        Assert.Equal(name, payload.GetProperty("name").GetString());
        Assert.Equal(3, payload.GetProperty("nested").GetProperty("keep").GetInt32());
        Assert.Equal(replaced, payload.GetProperty("nested").GetProperty("replace").GetInt32());
        JsonElement items = payload.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(1, items[0].GetProperty("values")[0].GetInt32());
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("values")[1].ValueKind);
        Assert.Equal(appended, Assert.Single(items[1].GetProperty("values").EnumerateArray()).GetInt32());
        Assert.False(payload.GetProperty("untouched")[0][0].GetBoolean());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("untouched")[1].ValueKind);
    }

    public interface SelectedMessage
    {
        Guid Id { get; }
    }

    private sealed class ForwardAdmissionObserver : ISendObserver
    {
        public ConcurrentQueue<(Uri? Address, string[] Types)> Messages { get; } = new();
        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            _ = Assert.IsAssignableFrom<TransportSendContext>(context).Body.Length;
            Messages.Enqueue((context.DestinationAddress, context.SupportedMessageTypes.ToArray()));
            return Task.CompletedTask;
        }
        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    public sealed class CompleteMessage : SelectedMessage
    {
        public Guid Id { get; init; }
        public JsonElement Extra { get; init; }
    }
}
