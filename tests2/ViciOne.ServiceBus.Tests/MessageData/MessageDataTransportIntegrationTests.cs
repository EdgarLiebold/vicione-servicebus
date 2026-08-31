using System.Text;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

[Collection(MessageDataDefaultsCollection.Name)]
public sealed class MessageDataTransportIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "inline-string-bytes-and-stored-stream")]
    public async Task BelowThreshold_StringAndBytesStayInlineWhileStreamUsesTheRepository()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = new MessageDataDefaultsScope();
        MessageDataDefaults.AlwaysWriteToRepository = false;
        MessageDataDefaults.Threshold = 4096;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-inline", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(async context =>
        {
            await using Stream stream = await context.Message.Stream.Value;
            observed.TrySetResult(new TransportSnapshot(
                context.Message.Text.Address,
                context.Message.Bytes.Address,
                context.Message.Stream.Address,
                await context.Message.Text.Value,
                await context.Message.Bytes.Value,
                await ReadBytes(stream, context.CancellationToken)));
        });
        byte[] bytes = [1, 2, 3, 4, 5];
        byte[] streamBytes = [6, 7, 8, 9];
        await using var source = new MemoryStream(streamBytes, writable: false);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send<TransportEnvelope>(new
            {
                Text = "inline",
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);

            Assert.Null(actual.TextAddress);
            Assert.Null(actual.BytesAddress);
            Assert.NotNull(actual.StreamAddress);
            Assert.Equal("inline", actual.Text);
            Assert.Equal(bytes, actual.Bytes);
            Assert.Equal(streamBytes, actual.StreamBytes);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "filesystem-stored-string-bytes-stream")]
    public async Task AboveThreshold_FileSystemTransportRoundTripsStringBytesAndStreamExactly()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = new MessageDataDefaultsScope();
        MessageDataDefaults.AlwaysWriteToRepository = true;
        MessageDataDefaults.Threshold = 1;
        DirectoryInfo directory = RunDirectory("stored");
        var repository = new FileSystemMessageDataRepository(directory);
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-stored", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(async context =>
        {
            await using Stream stream = await context.Message.Stream.Value;
            observed.TrySetResult(new TransportSnapshot(
                context.Message.Text.Address,
                context.Message.Bytes.Address,
                context.Message.Stream.Address,
                await context.Message.Text.Value,
                await context.Message.Bytes.Value,
                await ReadBytes(stream, context.CancellationToken)));
        });
        string text = $"stored-{NewId.NextGuid():N}";
        byte[] bytes = Enumerable.Range(0, 129).Select(index => (byte)(index % 127)).ToArray();
        byte[] streamBytes = Enumerable.Range(0, 73).Select(index => (byte)(index + 31)).ToArray();
        await using var source = new MemoryStream(streamBytes, writable: false);

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send<TransportEnvelope>(new
            {
                Text = text,
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);

            Assert.NotNull(actual.TextAddress);
            Assert.NotNull(actual.BytesAddress);
            Assert.NotNull(actual.StreamAddress);
            Assert.Equal(3, new[] { actual.TextAddress, actual.BytesAddress, actual.StreamAddress }.Distinct().Count());
            Assert.Equal(text, actual.Text);
            Assert.Equal(bytes, actual.Bytes);
            Assert.Equal(streamBytes, actual.StreamBytes);
            Assert.Equal(3, directory.EnumerateFiles("*", SearchOption.AllDirectories).Count());
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
            Delete(directory);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "encrypted-at-rest-string-bytes-stream")]
    public async Task EncryptedFileSystemTransport_RoundTripsEveryTypeWithoutWritingPlaintext()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = new MessageDataDefaultsScope();
        MessageDataDefaults.AlwaysWriteToRepository = true;
        MessageDataDefaults.Threshold = 1;
        DirectoryInfo directory = RunDirectory("encrypted");
        var repository = new EncryptedMessageDataRepository(
            new FileSystemMessageDataRepository(directory),
            new AesCryptoStreamProvider(new FixedSymmetricKeyProvider(), "default"));
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-encrypted", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(async context =>
        {
            await using Stream stream = await context.Message.Stream.Value;
            observed.TrySetResult(new TransportSnapshot(
                context.Message.Text.Address,
                context.Message.Bytes.Address,
                context.Message.Stream.Address,
                await context.Message.Text.Value,
                await context.Message.Bytes.Value,
                await ReadBytes(stream, context.CancellationToken)));
        });
        string text = $"secret-{NewId.NextGuid():N}";
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        byte[] bytes = Enumerable.Range(0, 131).Select(index => (byte)(index % 113)).ToArray();
        byte[] streamBytes = Enumerable.Range(0, 79).Select(index => (byte)(255 - index)).ToArray();
        await using var source = new MemoryStream(streamBytes, writable: false);

        try
        {
            await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send<TransportEnvelope>(new
            {
                Text = text,
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(text, actual.Text);
            Assert.Equal(bytes, actual.Bytes);
            Assert.Equal(streamBytes, actual.StreamBytes);
            byte[][] stored = directory.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => File.ReadAllBytes(file.FullName))
                .ToArray();
            Assert.Equal(3, stored.Length);
            Assert.DoesNotContain(stored, value => value.SequenceEqual(textBytes));
            Assert.DoesNotContain(stored, value => value.SequenceEqual(bytes));
            Assert.DoesNotContain(stored, value => value.SequenceEqual(streamBytes));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
            Delete(directory);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "stored-reference-and-nested-collection-graph")]
    public async Task StoredReferences_AreResolvedAcrossObjectArrayListDictionaryBytesAndStream()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<ReferenceSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-references", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<ReferenceEnvelope>(async context =>
        {
            await using Stream stream = await context.Message.Stream.Value;
            observed.TrySetResult(new ReferenceSnapshot(
                await context.Message.Text.Value,
                await context.Message.Bytes.Value,
                await ReadBytes(stream, context.CancellationToken),
                await context.Message.Document.Body.Value,
                await context.Message.Documents[0].Body.Value,
                await context.Message.DocumentList[0].Body.Value,
                await context.Message.DocumentIndex["first"].Body.Value,
                await context.Message.DocumentIndex["second"].Body.Value));
        });
        Guid identity = Guid.Parse("82d15d34-acde-4d71-9cf0-6155c3a534d2");
        byte[] identityBytes = identity.ToByteArray();
        MessageData<string> text = await repository.PutString(identity.ToString(), cancellationToken);
        MessageData<byte[]> bytes = await repository.PutBytes(identityBytes, cancellationToken);
        await using var source = new MemoryStream(identityBytes, writable: false);
        MessageData<Stream> stream = await repository.PutStream(source, cancellationToken);
        var document = new NestedDocument("root", bytes);
        var message = new ReferenceEnvelope(
            text,
            bytes,
            stream,
            document,
            [new NestedDocument("array", bytes)],
            [new NestedDocument("list", bytes)],
            new Dictionary<string, NestedDocument>
            {
                ["first"] = new("dictionary-1", bytes),
                ["second"] = new("dictionary-2", bytes),
            });

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            ReferenceSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(identity.ToString(), actual.Text);
            Assert.All(actual.ByteValues, value => Assert.Equal(identityBytes, value));
            Assert.Equal(7, actual.ByteValues.Count);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TOPOLOGY", "message-graph-without-data-has-no-transform")]
    public void MessageGraphWithoutMessageData_DoesNotCreateSendOrConsumeTransforms()
    {
        var repository = new InMemoryMessageDataRepository();
        var put = new PutMessageDataTransformSpecification<PlainEnvelope>(repository);
        var get = new GetMessageDataTransformSpecification<PlainEnvelope>(repository);

        Assert.False(put.TryGetConverter(out _));
        Assert.False(put.TryGetSendTopology(out _));
        Assert.False(get.TryGetConverter(out _));
        Assert.False(get.TryGetConsumeTopology(out _));
    }

    private static InMemoryTestHarness CreateHarness(
        string prefix,
        TimeSpan timeout,
        IMessageDataRepository repository)
    {
        var harness = new InMemoryTestHarness($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryBus += configurator => configurator.UseMessageData(repository);
        return harness;
    }

    private static async Task<byte[]> ReadBytes(Stream stream, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);
        return copy.ToArray();
    }

    private static DirectoryInfo RunDirectory(string suffix) =>
        new(Path.Combine(Path.GetTempPath(), "vsb-message-data", $"{suffix}-{NewId.NextGuid():N}"));

    private static void Delete(DirectoryInfo directory)
    {
        if (directory.Exists)
            directory.Delete(recursive: true);
    }

    public interface TransportEnvelope
    {
        MessageData<string> Text { get; }

        MessageData<byte[]> Bytes { get; }

        MessageData<Stream> Stream { get; }
    }

    private sealed record TransportSnapshot(
        Uri? TextAddress,
        Uri? BytesAddress,
        Uri? StreamAddress,
        string Text,
        byte[] Bytes,
        byte[] StreamBytes);

    private sealed record ReferenceEnvelope(
        MessageData<string> Text,
        MessageData<byte[]> Bytes,
        MessageData<Stream> Stream,
        NestedDocument Document,
        NestedDocument[] Documents,
        List<NestedDocument> DocumentList,
        Dictionary<string, NestedDocument> DocumentIndex);

    private sealed record NestedDocument(string Name, MessageData<byte[]> Body);

    private sealed record ReferenceSnapshot(
        string Text,
        byte[] Bytes,
        byte[] StreamBytes,
        byte[] ObjectBytes,
        byte[] ArrayBytes,
        byte[] ListBytes,
        byte[] DictionaryFirstBytes,
        byte[] DictionarySecondBytes)
    {
        public IReadOnlyList<byte[]> ByteValues =>
            [Bytes, StreamBytes, ObjectBytes, ArrayBytes, ListBytes, DictionaryFirstBytes, DictionarySecondBytes];
    }

    private sealed record PlainEnvelope(string Value, PlainChild Child, PlainChild[] Children);

    private sealed record PlainChild(int Number);

    private sealed class FixedSymmetricKeyProvider : ISymmetricKeyProvider
    {
        private readonly SymmetricKey _key = new FixedSymmetricKey();

        public bool TryGetKey(string id, out SymmetricKey key)
        {
            key = _key;
            return true;
        }
    }

    private sealed class FixedSymmetricKey : SymmetricKey
    {
        public byte[] Key { get; } = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();

        public byte[] IV { get; } = Enumerable.Range(101, 16).Select(value => (byte)value).ToArray();
    }
}
