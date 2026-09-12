using System.Text;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class MessageDataPropertyConverterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-MESSAGE-DATA", "binary-and-text-threshold-matrix")]
    public async Task BinaryAndTextInputs_SelectTheOwnedRepresentationAtTheExactThresholdAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        var converter = MessageDataPropertyConverter.Instance;

        byte[] source = [1, 2, 3];
        MessageData<byte[]>? binary = await converter.ConvertAsync(context, source, token);
        var deferredBinary = Assert.IsType<PutMessageData<byte[]>>(binary);
        source[0] = 9;
        Assert.Equal([1, 2, 3], await deferredBinary.Value);
        Assert.Null(await converter.ConvertAsync(context, (byte[]?)null, token));

        IPropertyConverter<MessageData<byte[]>, string> textConverter = converter;
        string belowThreshold = new('a', MessageDataPolicy.Default.Threshold - 1);
        string atThreshold = new('b', MessageDataPolicy.Default.Threshold);
        MessageData<byte[]>? inline = await textConverter.ConvertAsync(context, belowThreshold, token);
        MessageData<byte[]>? deferred = await textConverter.ConvertAsync(context, atThreshold, token);

        Assert.IsType<BytesInlineMessageData>(inline);
        Assert.Equal(Encoding.UTF8.GetBytes(belowThreshold), await inline!.Value);
        Assert.Null(inline.Address);
        Assert.IsType<PutMessageData<byte[]>>(deferred);
        Assert.Equal(Encoding.UTF8.GetBytes(atThreshold), await deferred!.Value);
        Assert.Null(await textConverter.ConvertAsync(context, null, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-MESSAGE-DATA", "existing-stream-string-and-object-matrix")]
    public async Task ExistingStreamStringAndObjectInputs_ArePreservedOrWrappedWithoutFeatureLossAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        var converter = MessageDataPropertyConverter.Instance;

        MessageData<byte[]> existingBinary = new PutMessageData<byte[]>([4, 5]);
        Assert.Same(existingBinary, await converter.ConvertAsync(context, existingBinary, token));
        Assert.Null(await converter.ConvertAsync(context, (MessageData<byte[]>?)null, token));

        await using var stream = new MemoryStream([6, 7]);
        MessageData<Stream>? streamValue = await converter.ConvertAsync(context, stream, token);
        Assert.IsType<PutMessageData<Stream>>(streamValue);
        Assert.Same(stream, await streamValue!.Value);
        MessageData<Stream> existingStream = streamValue;
        Assert.Same(existingStream, await converter.ConvertAsync(context, existingStream, token));
        Assert.Null(await converter.ConvertAsync(context, (Stream?)null, token));
        Assert.Null(await converter.ConvertAsync(context, (MessageData<Stream>?)null, token));

        IPropertyConverter<MessageData<string>, string> stringConverter = converter;
        IPropertyConverter<MessageData<string>, MessageData<string>> existingStringConverter = converter;
        MessageData<string>? stringValue = await stringConverter.ConvertAsync(context, "value", token);
        Assert.IsType<PutMessageData<string>>(stringValue);
        Assert.Equal("value", await stringValue!.Value);
        Assert.Same(stringValue, await existingStringConverter.ConvertAsync(context, stringValue, token));
        Assert.Null(await stringConverter.ConvertAsync(context, null, token));
        Assert.Null(await existingStringConverter.ConvertAsync(context, null, token));

        var objectConverter = new MessageDataPropertyConverter<TestPayload>();
        var payload = new TestPayload("owned");
        MessageData<TestPayload>? objectValue = await objectConverter.ConvertAsync(context, payload, token);
        Assert.IsType<PutMessageData<TestPayload>>(objectValue);
        Assert.Same(payload, await objectValue!.Value);
        Assert.Same(objectValue, await objectConverter.ConvertAsync(context, objectValue, token));
        Assert.Null(await objectConverter.ConvertAsync(context, (TestPayload?)null, token));
        Assert.Null(await objectConverter.ConvertAsync(context, (MessageData<TestPayload>?)null, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-MESSAGE-DATA", "string-message-data-state-matrix")]
    public async Task StringMessageDataConversion_CoversAddressValueTimingFailureAndCancellationStatesAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        IPropertyConverter<MessageData<byte[]>, MessageData<string>> converter = MessageDataPropertyConverter.Instance;
        var address = new Uri("urn:message-data:initializer");

        var empty = new StubMessageData<string>(false, address, Task.FromException<string?>(new InvalidOperationException("must not read")));
        Assert.Null(await converter.ConvertAsync(context, empty, token));
        Assert.Equal(0, empty.ValueReads);

        var inlineInput = new StubMessageData<string>(true, address, Task.FromResult<string?>("inline"));
        MessageData<byte[]>? inline = await converter.ConvertAsync(context, inlineInput, token);
        var inlineValue = Assert.IsType<BytesInlineMessageData>(inline);
        Assert.Equal(address, inlineValue.Address);
        Assert.Equal(Encoding.UTF8.GetBytes("inline"), await inlineValue.Value);

        string largeText = new('x', MessageDataPolicy.Default.Threshold);
        var addressedInput = new StubMessageData<string>(true, address, Task.FromResult<string?>(largeText));
        MessageData<byte[]>? stored = await converter.ConvertAsync(context, addressedInput, token);
        var storedValue = Assert.IsType<StoredMessageData<byte[]>>(stored);
        Assert.Equal(address, storedValue.Address);
        Assert.Equal(Encoding.UTF8.GetBytes(largeText), await storedValue.Value);

        var addresslessInput = new StubMessageData<string>(true, null, Task.FromResult<string?>(largeText));
        Assert.IsType<PutMessageData<byte[]>>(await converter.ConvertAsync(context, addresslessInput, token));

        var pendingSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingInput = new StubMessageData<string>(true, address, pendingSource.Task);
        Task<MessageData<byte[]>?> pendingConversion = converter.ConvertAsync(context, pendingInput, token);
        Assert.False(pendingConversion.IsCompleted);
        pendingSource.SetResult("later");
        Assert.IsType<BytesInlineMessageData>(await pendingConversion);

        var expected = new ExpectedMessageDataException("source failed");
        var faulted = new StubMessageData<string>(true, address, Task.FromException<string?>(expected));
        ExpectedMessageDataException sourceFailure = await Assert.ThrowsAsync<ExpectedMessageDataException>(() =>
            converter.ConvertAsync(context, faulted, token));
        Assert.Same(expected, sourceFailure);

        var missingValue = new StubMessageData<string>(true, address, Task.FromResult<string?>(null));
        MessageDataException missingValueFailure = await Assert.ThrowsAsync<MessageDataException>(() =>
            converter.ConvertAsync(context, missingValue, token));
        Assert.Equal("The message data reference reported a value but returned null.", missingValueFailure.Message);

        var missingTask = new StubMessageData<string>(true, address, null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => converter.ConvertAsync(context, missingTask, token));

        using var callerCancellation = new CancellationTokenSource();
        var neverCompletes = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<MessageData<byte[]>?> canceledConversion = converter.ConvertAsync(
            context, new StubMessageData<string>(true, address, neverCompletes.Task), callerCancellation.Token);
        callerCancellation.Cancel();
        OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledConversion);
        Assert.Equal(callerCancellation.Token, cancellation.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-MESSAGE-DATA", "context-and-pre-cancellation-boundaries")]
    public async Task EveryMessageDataConversion_ValidatesContextAndPreCancellationBeforeInputAccessAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        var converter = MessageDataPropertyConverter.Instance;
        var objectConverter = new MessageDataPropertyConverter<TestPayload>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            converter.ConvertAsync<TestMessage>(null!, Array.Empty<byte>(), cancellation.Token))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            objectConverter.ConvertAsync<TestMessage>(null!, new TestPayload("value"), cancellation.Token))).ParamName);

        await AssertCanceledAsync(() => converter.ConvertAsync(context, Array.Empty<byte>(), cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => converter.ConvertAsync(context, (MessageData<byte[]>?)null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => converter.ConvertAsync(context, Stream.Null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => converter.ConvertAsync(context, (MessageData<Stream>?)null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => ((IPropertyConverter<MessageData<byte[]>, string>)converter)
            .ConvertAsync(context, null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => ((IPropertyConverter<MessageData<byte[]>, MessageData<string>>)converter)
            .ConvertAsync(context, new ThrowingMessageData<string>(), cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => ((IPropertyConverter<MessageData<string>, string>)converter)
            .ConvertAsync(context, null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => ((IPropertyConverter<MessageData<string>, MessageData<string>>)converter)
            .ConvertAsync(context, null, cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => objectConverter.ConvertAsync(context, new TestPayload("value"), cancellation.Token), cancellation.Token);
        await AssertCanceledAsync(() => objectConverter.ConvertAsync(context, (MessageData<TestPayload>?)null, cancellation.Token), cancellation.Token);
    }

    static async Task AssertCanceledAsync(Func<Task> operation, CancellationToken expectedToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(operation);
        Assert.Equal(expectedToken, exception.CancellationToken);
    }

    static InitializeContext<TestMessage> CreateContext() =>
        new BaseInitializeContext(TestContext.Current.CancellationToken).CreateMessageContext(new TestMessage());

    private sealed class StubMessageData<T>(bool hasValue, Uri? address, Task<T?> value) : MessageData<T>
    {
        public Uri? Address { get; } = address;

        public bool HasValue { get; } = hasValue;

        public int ValueReads { get; private set; }

        public Task<T?> Value
        {
            get
            {
                ValueReads++;
                return value;
            }
        }
    }

    private sealed class ThrowingMessageData<T> : MessageData<T>
    {
        public Uri? Address => throw new InvalidOperationException("must not read");

        public bool HasValue => throw new InvalidOperationException("must not read");

        public Task<T?> Value => throw new InvalidOperationException("must not read");
    }

    private sealed record TestPayload(string Value);

    private sealed class TestMessage;

    private sealed class ExpectedMessageDataException(string message) : Exception(message);
}
