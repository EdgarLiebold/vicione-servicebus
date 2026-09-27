using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class JsonValueReceiveBoundaryTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    public static TheoryData<string, decimal> DecimalValues => new()
    {
        { "null", 0m },
        { "\"\"", 0m },
        { "\"   \"", 0m },
        { "-19.75", -19.75m },
        { "\"  +1,234.50  \"", 1234.50m },
        { "\"125-\"", -125m },
        { "\"1.25e2\"", 125m },
        { "-79228162514264337593543950335", decimal.MinValue },
        { "\"79228162514264337593543950335\"", decimal.MaxValue },
        { "0.0000000000000000000000000001", 0.0000000000000000000000000001m },
    };

    [Theory]
    [MemberData(nameof(DecimalValues))]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DECIMAL", "receive-invariant-boundaries-and-canonical-output")]
    public async Task DecimalWireValues_PreserveInvariantMeaningAndCanonicalOutputAsync(string jsonValue, decimal expected)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        var received = new TaskCompletionSource<JsonBoundaryMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faults = new ConcurrentQueue<ReceiveFault>();
        using var harness = CreateHarness(context => received.TrySetResult(context.Message), faults);
        try
        {
            await harness.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Guid messageId = NewId.NextGuid();
            await SendAsync(harness, messageId, $"\"amount\":{jsonValue}");
            JsonBoundaryMessage message = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal(expected, message.Amount);
            var serializer = new SystemTextJsonMessageSerializer(ServiceBusMetadataJson.Options);
            using JsonDocument canonical = JsonDocument.Parse(serializer.SerializeObject(message).ToArray());
            JsonElement amount = canonical.RootElement.GetProperty("amount");
            Assert.Equal(JsonValueKind.String, amount.ValueKind);
            Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), amount.GetString());
        }
        finally
        {
            try
            {
                await harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
        Assert.Empty(faults);
    }

    [Theory]
    [InlineData("\"amount\":79228162514264337593543950336")]
    [InlineData("\"amount\":\"79228162514264337593543950336\"")]
    [InlineData("\"amount\":1e100")]
    [InlineData("\"amount\":\"not-a-number\"")]
    [InlineData("\"amount\":true")]
    [InlineData("\"amount\":[]")]
    [InlineData("\"amount\":{}")]
    [InlineData("\"quantities\":[]")]
    [InlineData("\"quantities\":{\"\":1}")]
    [InlineData("\"quantities\":{\"order\":false}")]
    [InlineData("\"routes\":[]")]
    [InlineData("\"routes\":{\"   \":1}")]
    [InlineData("\"routes\":{\"http://[::1\":1}")]
    [InlineData("\"metadata\":{\"\":1}")]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "json-value-failure-isolates-next-delivery")]
    public async Task MalformedPayload_RejectsTheDeclaredContractWithoutContaminatingTheNextMessageAsync(string property)
    {
        var delivered = new ConcurrentQueue<(Guid? Id, JsonBoundaryMessage Message)>();
        var healthy = new TaskCompletionSource<JsonBoundaryMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faults = new ConcurrentQueue<ReceiveFault>();
        using var harness = CreateHarness(context =>
        {
            delivered.Enqueue((context.MessageId, context.Message));
            healthy.TrySetResult(context.Message);
        }, faults, context => failure.TrySetResult(context));
        Guid rejectedId = NewId.NextGuid();
        Guid healthyId = NewId.NextGuid();
        try
        {
            await harness.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await SendAsync(harness, rejectedId, property);
            Task terminal = await Task.WhenAny(failure.Task, healthy.Task).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Same(failure.Task, terminal);
            ConsumeContext<ReceiveFault> fault = await failure.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(rejectedId, fault.Message.FaultedMessageId);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonMediaType, fault.Message.ContentType);
            Assert.Equal(harness.InputQueueAddress, fault.SourceAddress);
            ExceptionInfo exception = Assert.Single(fault.Message.Exceptions);
            Assert.Equal(TypeCache<JsonException>.ShortName, exception.ExceptionType);
            Assert.False(healthy.Task.IsCompleted);
            Assert.Empty(delivered);

            await SendAsync(harness, healthyId, """
                "amount":"19.75","quantities":{"Order":3},
                "routes":{"loopback://localhost/healthy":7},"metadata":{"Marker":"healthy"}
                """);
            JsonBoundaryMessage message = await healthy.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(19.75m, message.Amount);
            Assert.Equal(3, Assert.Single(message.Quantities!).Value);
            Assert.Equal(3, message.Quantities!["ORDER"]);
            Assert.Equal(new Uri("loopback://localhost/healthy"), Assert.Single(message.Routes!).Key);
            Assert.Equal(7, Assert.Single(message.Routes!).Value);
            Assert.Equal("healthy", Assert.Single(message.Metadata!).Value);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Equal(healthyId, Assert.Single(delivered).Id);
        Assert.Equal(rejectedId, Assert.Single(faults).FaultedMessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DICTIONARY", "received-nested-dictionaries-preserve-shapes")]
    public async Task NestedDictionary_RetainsTypedValuesAndCaseInsensitiveLookupAsync()
    {
        var received = new TaskCompletionSource<JsonBoundaryMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faults = new ConcurrentQueue<ReceiveFault>();
        using var harness = CreateHarness(context => received.TrySetResult(context.Message), faults);
        try
        {
            await harness.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await SendAsync(harness, NewId.NextGuid(), """
                "amount":"12.5",
                "quantities":{"Order":1,"order":4,"Empty":0},
                "routes":{"relative/path":2,"loopback://localhost/orders":9},
                "metadata":[
                  {"Key":"Tree","Value":{"Name":"kept","Items":[{"Id":8},[true,null,"tail"]]}},
                  {"Key":"Trace","Value":"first"},{"Key":"trace","Value":"last"}
                ]
                """);
            JsonBoundaryMessage message = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(12.5m, message.Amount);
            Assert.Equal(2, message.Quantities!.Count);
            Assert.Equal(4, message.Quantities["ORDER"]);
            Assert.Equal(0, message.Quantities["EMPTY"]);
            Assert.Equal(2, message.Routes!.Count);
            Assert.Equal(2, message.Routes[new Uri("relative/path", UriKind.Relative)]);
            Assert.Equal(9, message.Routes[new Uri("loopback://localhost/orders")]);
            Assert.Equal(2, message.Metadata!.Count);
            Assert.Equal("last", message.Metadata["TRACE"]);
            var tree = Assert.IsType<Dictionary<string, object>>(message.Metadata["TREE"]);
            Assert.Equal("kept", tree["NAME"]);
            var items = Assert.IsType<List<object>>(tree["ITEMS"]);
            Assert.Equal(2, items.Count);
            Assert.Equal(8L, Assert.Single(Assert.IsType<Dictionary<string, object>>(items[0])).Value);
            Assert.Collection(Assert.IsType<List<object>>(items[1]),
                value => Assert.True(Assert.IsType<bool>(value)),
                Assert.Null,
                value => Assert.Equal("tail", value));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Empty(faults);
    }

    private static InMemoryTestHarness CreateHarness(Action<ConsumeContext<JsonBoundaryMessage>> consume,
        ConcurrentQueue<ReceiveFault> faults, Action<ConsumeContext<ReceiveFault>>? faulted = null)
    {
        var harness = new InMemoryTestHarness($"json-boundary-{NewId.NextGuid():N}") { TestTimeout = Timeout };
        harness.BeginTestScope();
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<JsonBoundaryMessage>(context =>
            {
                consume(context);
                return Task.CompletedTask;
            });
            endpoint.Handler<ReceiveFault>(context =>
            {
                faults.Enqueue(context.Message);
                faulted?.Invoke(context);
                return Task.CompletedTask;
            });
        };
        return harness;
    }

    private static Task SendAsync(InMemoryTestHarness harness, Guid messageId, string properties)
    {
        string envelope = $$"""
            {"messageId":"{{messageId:D}}","messageTypes":["{{MessageUrn.ForTypeString<JsonBoundaryMessage>()}}"],
             "message":{ {{properties}} } }
            """;
        return harness.InputQueueSendEndpoint.SendAsync(new JsonBoundaryMessage(), context =>
        {
            context.MessageId = messageId;
            context.Serializer = new CopyBodySerializer(SystemTextJsonMessageSerializer.JsonContentType, new StringMessageBody(envelope));
        }, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }

    public sealed class JsonBoundaryMessage
    {
        public decimal Amount { get; init; }
        public IDictionary<string, int>? Quantities { get; init; }
        public IDictionary<Uri, int>? Routes { get; init; }
        public IDictionary<string, object>? Metadata { get; init; }
    }
}
