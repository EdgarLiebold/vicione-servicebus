using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataInitializerIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-INITIALIZER", "interface-and-class-complete-conversion-matrix")]
    public async Task Initializer_RoundTripsEveryPropertyAndReusesRepositoryAddressesAsync(bool concreteContract)
    {
        if (concreteContract)
            await RoundTripEveryPropertyAsync<ClassProcessDocument>();
        else
            await RoundTripEveryPropertyAsync<InterfaceProcessDocument>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-INITIALIZER", "interface-and-class-missing-data-fault")]
    public async Task Initializer_MissingRequiredMessageDataProducesTheExactRequestFaultAsync(bool concreteContract)
    {
        if (concreteContract)
            await MissingDataFaultsAsync<ClassProcessDocument>();
        else
            await MissingDataFaultsAsync<InterfaceProcessDocument>();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-OBJECT", "application-object-address-and-false-dictionary-value")]
    public async Task ApplicationObject_RoundTripsItsAddressAndEveryDictionaryValueIncludingFalseAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observedAddress = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-object", timeout, repository, StoredPolicy());
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<ObjectRequest>(async context =>
        {
            SpecialPayload value = MessageDataTestSupport.Require(await context.Message.Payload.Value, nameof(context.Message.Payload));
            observedAddress.TrySetResult(MessageDataTestSupport.Require(context.Message.Payload.Address, nameof(context.Message.Payload)));
            await context.Advanced().RespondAsync<ObjectResponse>(new { context.Message.Payload });
        });
        var expected = new SpecialPayload(
            "object-payload",
            new Dictionary<string, object>
            {
                ["text"] = "value",
                ["true"] = true,
                ["false"] = false,
            });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<ObjectRequest> client =
                harness.Bus.CreateRequestClient<ObjectRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));
            Response<ObjectResponse> response = await client.Advanced().GetResponseAsync<ObjectResponse>(
                values: new { Payload = expected },
                cancellationToken: cancellationToken);
            Uri consumedAddress = await observedAddress.Task.WaitAsync(timeout, cancellationToken);
            SpecialPayload actual = MessageDataTestSupport.Require(await response.Message.Payload.Value, nameof(response.Message.Payload));
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(consumedAddress, response.Message.Payload.Address);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal("value", JsonValue(actual.Dictionary["text"]));
            Assert.True(bool.Parse(JsonValue(actual.Dictionary["true"])));
            Assert.False(bool.Parse(JsonValue(actual.Dictionary["false"])));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-INITIALIZER", "nested-array-exact-file-and-body")]
    public async Task NestedArrayInitializer_StoresAndResolvesEveryDocumentBodyAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<NestedSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-nested-array", timeout, repository, StoredPolicy());
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<Documents>(async context =>
        {
            observed.TrySetResult(new NestedSnapshot(
                context.Message.Bodies.Select(document => document.FileName).ToArray(),
                (await Task.WhenAll(context.Message.Bodies.Select(document => document.Body.Value)))
                .Select((body, index) => MessageDataTestSupport.Require(body, $"Bodies[{index}].Body"))
                .ToArray(),
                context.Message.Bodies
                .Select((document, index) => MessageDataTestSupport.Require(document.Body.Address, $"Bodies[{index}].Body.Address"))
                .ToArray()));
        });
        byte[] first = Enumerable.Range(0, 10_000).Select(index => (byte)(index % 251)).ToArray();
        byte[] second = Enumerable.Range(0, 10_000).Select(index => (byte)(250 - index % 251)).ToArray();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync<Documents>(new
            {
                Bodies = new[]
                {
                    new { FileName = "first.txt", Body = first },
                    new { FileName = "second.txt", Body = second },
                },
            }, cancellationToken);
            NestedSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(["first.txt", "second.txt"], actual.FileNames);
            Assert.Equal(first, actual.Bodies[0]);
            Assert.Equal(second, actual.Bodies[1]);
            Assert.All(actual.Addresses, Assert.NotNull);
            Assert.Equal(2, actual.Addresses.Distinct().Count());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task RoundTripEveryPropertyAsync<TRequest>()
        where TRequest : class, IProcessDocument
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<InputAddresses>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-initializer", timeout, repository, StoredPolicy());
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TRequest>(async context =>
        {
            if (context.Message.StringData is not { HasValue: true })
                throw new MessageDataException("StringData was required.");

            observed.TrySetResult(new InputAddresses(
                MessageDataTestSupport.Require(context.Message.StringData.Address, nameof(context.Message.StringData)),
                MessageDataTestSupport.Require(context.Message.ByteData.Address, nameof(context.Message.ByteData)),
                MessageDataTestSupport.Require(context.Message.StreamData.Address, nameof(context.Message.StreamData))));
            await context.Advanced().RespondAsync<ProcessedDocument>(new
            {
                context.Message.CorrelationId,
                context.Message.StringData,
                StringByteData = context.Message.StringData,
                context.Message.ByteData,
                context.Message.StringValue,
                StringByteValue = context.Message.StringValue,
                context.Message.ByteValue,
                context.Message.StreamData,
            });
        });
        Guid correlationId = Guid.Parse("117b4c51-10c5-42fc-8fc5-dd16e7fef8c9");
        const string stringData = "stored string data";
        const string byteData = "stored byte data";
        const string stringValue = "promoted string value";
        const string byteValue = "promoted byte value";
        byte[] streamBytes = Enumerable.Range(0, 1000).Select(index => (byte)(index % 241)).ToArray();
        await using var source = new MemoryStream(streamBytes, writable: false);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<TRequest> client = harness.Bus.CreateRequestClient<TRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));
            Response<ProcessedDocument> response = await client.Advanced().GetResponseAsync<ProcessedDocument>(values: new
            {
                CorrelationId = correlationId,
                StringData = stringData,
                ByteData = byteData,
                StringValue = stringValue,
                ByteValue = Encoding.UTF8.GetBytes(byteValue),
                StreamData = source,
            }, cancellationToken: cancellationToken);
            InputAddresses input = await observed.Task.WaitAsync(timeout, cancellationToken);
            await using Stream returnedStream = MessageDataTestSupport.Require(
                await response.Message.StreamData.Value, nameof(response.Message.StreamData));
            byte[] returnedStreamBytes = await ReadBytesAsync(returnedStream, cancellationToken);
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(input.StringData, response.Message.StringData.Address);
            Assert.Equal(input.StringData, response.Message.StringByteData.Address);
            Assert.Equal(input.ByteData, response.Message.ByteData.Address);
            Assert.Equal(input.StreamData, response.Message.StreamData.Address);
            Assert.Equal(stringData, await response.Message.StringData.Value);
            Assert.Equal(stringData, Encoding.UTF8.GetString(MessageDataTestSupport.Require(
                await response.Message.StringByteData.Value, nameof(response.Message.StringByteData))));
            Assert.Equal(byteData, Encoding.UTF8.GetString(MessageDataTestSupport.Require(
                await response.Message.ByteData.Value, nameof(response.Message.ByteData))));
            Assert.Equal(stringValue, await response.Message.StringValue.Value);
            Assert.Equal(stringValue, Encoding.UTF8.GetString(MessageDataTestSupport.Require(
                await response.Message.StringByteValue.Value, nameof(response.Message.StringByteValue))));
            Assert.Equal(byteValue, Encoding.UTF8.GetString(MessageDataTestSupport.Require(
                await response.Message.ByteValue.Value, nameof(response.Message.ByteValue))));
            Assert.Equal(streamBytes, returnedStreamBytes);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task MissingDataFaultsAsync<TRequest>()
        where TRequest : class, IProcessDocument
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        using var harness = CreateHarness("message-data-missing", timeout, repository, StoredPolicy());
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TRequest>(context =>
        {
            if (context.Message.StringData is not { HasValue: true })
                throw new MessageDataException("StringData was required.");

            return context.Advanced().RespondAsync<ProcessedDocument>(new { context.Message.StringData });
        });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IRequestClient<TRequest> client = harness.Bus.CreateRequestClient<TRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));
            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<ProcessedDocument>(values: new
                {
                    CorrelationId = Guid.Parse("5d54bc39-28fb-480d-868b-99144c9f8df8"),
                    ByteData = "bytes",
                    StringValue = "value",
                    ByteValue = Encoding.UTF8.GetBytes("byte-value"),
                }, cancellationToken: cancellationToken));
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);

            Assert.NotNull(exception.Fault);
            ExceptionInfo fault = Assert.Single(exception.Fault.Exceptions);
            Assert.Equal(typeof(MessageDataException).FullName, fault.ExceptionType);
            Assert.Equal("StringData was required.", fault.Message);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static MessageDataPolicy StoredPolicy() => new(alwaysWriteToRepository: true, threshold: 1);

    private static InMemoryTestHarness CreateHarness(
        string prefix,
        TimeSpan timeout,
        IMessageDataRepository repository,
        MessageDataPolicy policy)
    {
        var harness = new InMemoryTestHarness($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryBus += configurator => configurator.UseMessageData(repository, policy);
        return harness;
    }

    private static async Task<byte[]> ReadBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);
        return copy.ToArray();
    }

    private static string JsonValue(object value) => value is JsonElement element
        ? element.ValueKind == JsonValueKind.String
            ? element.GetString()!
            : element.GetRawText()
        : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;

    public interface IProcessDocument
    {
        Guid CorrelationId { get; }

        MessageData<string> StringData { get; }

        MessageData<byte[]> ByteData { get; }

        string StringValue { get; }

        byte[] ByteValue { get; }

        MessageData<Stream> StreamData { get; }
    }

    public interface InterfaceProcessDocument : IProcessDocument;

    public sealed class ClassProcessDocument : IProcessDocument
    {
        public Guid CorrelationId { get; set; }

        public MessageData<string> StringData { get; set; } = null!;

        public MessageData<byte[]> ByteData { get; set; } = null!;

        public string StringValue { get; set; } = null!;

        public byte[] ByteValue { get; set; } = null!;

        public MessageData<Stream> StreamData { get; set; } = null!;
    }

    public interface ProcessedDocument
    {
        Guid CorrelationId { get; }

        MessageData<string> StringData { get; }

        MessageData<byte[]> StringByteData { get; }

        MessageData<byte[]> ByteData { get; }

        MessageData<string> StringValue { get; }

        MessageData<byte[]> StringByteValue { get; }

        MessageData<byte[]> ByteValue { get; }

        MessageData<Stream> StreamData { get; }
    }

    public interface ObjectRequest
    {
        MessageData<SpecialPayload> Payload { get; }
    }

    public interface ObjectResponse
    {
        MessageData<SpecialPayload> Payload { get; }
    }

    public sealed record SpecialPayload(string Value, Dictionary<string, object> Dictionary);

    public interface Documents
    {
        Document[] Bodies { get; }
    }

    public interface Document
    {
        string FileName { get; }

        MessageData<byte[]> Body { get; }
    }

    private sealed record InputAddresses(Uri StringData, Uri ByteData, Uri StreamData);

    private sealed record NestedSnapshot(string[] FileNames, byte[][] Bodies, Uri[] Addresses);
}
