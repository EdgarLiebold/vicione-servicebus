using System.Text;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataTransportIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "duplicate-owner-configuration-rejected")]
    public void SameBus_RejectsASecondMessageDataOwnerConfiguration()
    {
        var first = new InMemoryMessageDataRepository();
        var second = new InMemoryMessageDataRepository();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            Bus.Factory.CreateUsingInMemory(configurator =>
            {
                configurator.UseMessageData(first, new MessageDataPolicy(alwaysWriteToRepository: false));
                configurator.UseMessageData(second, new MessageDataPolicy(alwaysWriteToRepository: true));
            }));

        Assert.Equal("Message data is already configured for this bus owner.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "same-contract-isolated-across-two-buses")]
    public async Task TwoBuses_ApplyOppositePoliciesToTheSameContractWithoutCrossTalkAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inlineRepository = new InMemoryMessageDataRepository();
        var storedRepository = new InMemoryMessageDataRepository();
        var inlineObserved = new TaskCompletionSource<Uri?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var storedObserved = new TaskCompletionSource<Uri?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var inlineHarness = CreateHarness(
            "message-data-policy-inline",
            timeout,
            inlineRepository,
            new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 4096));
        using var storedHarness = CreateHarness(
            "message-data-policy-stored",
            timeout,
            storedRepository,
            new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1));
        inlineHarness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<PolicyEnvelope>(context =>
        {
            inlineObserved.TrySetResult(context.Message.Text.Address);
            return Task.CompletedTask;
        });
        storedHarness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<PolicyEnvelope>(context =>
        {
            storedObserved.TrySetResult(context.Message.Text.Address);
            return Task.CompletedTask;
        });

        await inlineHarness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        await storedHarness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await inlineHarness.InputQueueSendEndpoint.SendAsync<PolicyEnvelope>(new { Text = "small" }, cancellationToken);
            await storedHarness.InputQueueSendEndpoint.SendAsync<PolicyEnvelope>(new { Text = "small" }, cancellationToken);

            Assert.Null(await inlineObserved.Task.WaitAsync(timeout, cancellationToken));
            Assert.NotNull(await storedObserved.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await storedHarness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            await inlineHarness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "inline-string-bytes-and-stored-stream")]
    public async Task BelowThreshold_StringAndBytesStayInlineWhileStreamUsesTheRepositoryAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var policy = new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 4096);
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-inline", timeout, repository, policy);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(context =>
            ObserveAsync(observed, async () =>
            {
                await using Stream stream = MessageDataTestSupport.Require(await context.Message.Stream.Value, nameof(context.Message.Stream));
                return new TransportSnapshot(
                    context.Message.Text.Address,
                    context.Message.Bytes.Address,
                    context.Message.Stream.Address,
                    MessageDataTestSupport.Require(await context.Message.Text.Value, nameof(context.Message.Text)),
                    MessageDataTestSupport.Require(await context.Message.Bytes.Value, nameof(context.Message.Bytes)),
                    await ReadBytesAsync(stream, context.CancellationToken));
            }));
        byte[] bytes = [1, 2, 3, 4, 5];
        byte[] streamBytes = [6, 7, 8, 9];
        await using var source = new MemoryStream(streamBytes, writable: false);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync<TransportEnvelope>(new
            {
                Text = "inline",
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

            Assert.Null(actual.TextAddress);
            Assert.Null(actual.BytesAddress);
            Assert.NotNull(actual.StreamAddress);
            Assert.Equal("inline", actual.Text);
            Assert.Equal(bytes, actual.Bytes);
            Assert.Equal(streamBytes, actual.StreamBytes);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "filesystem-stored-string-bytes-stream")]
    public async Task AboveThreshold_FileSystemTransportRoundTripsStringBytesAndStreamExactlyAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var policy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);
        DirectoryInfo directory = RunDirectory("stored");
        var repository = new FileSystemMessageDataRepository(directory);
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-stored", timeout, repository, policy);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(context =>
            ObserveAsync(observed, async () =>
            {
                await using Stream stream = MessageDataTestSupport.Require(await context.Message.Stream.Value, nameof(context.Message.Stream));
                return new TransportSnapshot(
                    context.Message.Text.Address,
                    context.Message.Bytes.Address,
                    context.Message.Stream.Address,
                    MessageDataTestSupport.Require(await context.Message.Text.Value, nameof(context.Message.Text)),
                    MessageDataTestSupport.Require(await context.Message.Bytes.Value, nameof(context.Message.Bytes)),
                    await ReadBytesAsync(stream, context.CancellationToken));
            }));
        string text = $"stored-{NewId.NextGuid():N}";
        byte[] bytes = Enumerable.Range(0, 129).Select(index => (byte)(index % 127)).ToArray();
        byte[] streamBytes = Enumerable.Range(0, 73).Select(index => (byte)(index + 31)).ToArray();
        await using var source = new MemoryStream(streamBytes, writable: false);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync<TransportEnvelope>(new
            {
                Text = text,
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

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
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            Delete(directory);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "encrypted-at-rest-string-bytes-stream")]
    public async Task EncryptedFileSystemTransport_RoundTripsEveryTypeWithoutWritingPlaintextAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var policy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);
        DirectoryInfo directory = RunDirectory("encrypted");
        var repository = new EncryptedMessageDataRepository(
            new FileSystemMessageDataRepository(directory),
            new AesCryptoStreamProvider(new FixedSymmetricKeyProvider(), "default"));
        var observed = new TaskCompletionSource<TransportSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-encrypted", timeout, repository, policy);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<TransportEnvelope>(context =>
            ObserveAsync(observed, async () =>
            {
                await using Stream stream = MessageDataTestSupport.Require(await context.Message.Stream.Value, nameof(context.Message.Stream));
                return new TransportSnapshot(
                    context.Message.Text.Address,
                    context.Message.Bytes.Address,
                    context.Message.Stream.Address,
                    MessageDataTestSupport.Require(await context.Message.Text.Value, nameof(context.Message.Text)),
                    MessageDataTestSupport.Require(await context.Message.Bytes.Value, nameof(context.Message.Bytes)),
                    await ReadBytesAsync(stream, context.CancellationToken));
            }));
        string text = $"secret-{NewId.NextGuid():N}";
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        byte[] bytes = Enumerable.Range(0, 131).Select(index => (byte)(index % 113)).ToArray();
        byte[] streamBytes = Enumerable.Range(0, 79).Select(index => (byte)(255 - index)).ToArray();
        await using var source = new MemoryStream(streamBytes, writable: false);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync<TransportEnvelope>(new
            {
                Text = text,
                Bytes = bytes,
                Stream = source,
            }, cancellationToken);
            TransportSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

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
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            Delete(directory);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TRANSPORT", "stored-reference-and-nested-collection-graph")]
    public async Task StoredReferences_AreResolvedAcrossObjectArrayListDictionaryBytesAndStreamAsync()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<ReferenceSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("message-data-references", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<ReferenceEnvelope>(context =>
            ObserveAsync(observed, async () =>
            {
                await using Stream stream = MessageDataTestSupport.Require(await context.Message.Stream.Value, nameof(context.Message.Stream));
                return new ReferenceSnapshot(
                    MessageDataTestSupport.Require(await context.Message.Text.Value, nameof(context.Message.Text)),
                    MessageDataTestSupport.Require(await context.Message.Bytes.Value, nameof(context.Message.Bytes)),
                    await ReadBytesAsync(stream, context.CancellationToken),
                    MessageDataTestSupport.Require(await context.Message.Document.Body.Value, nameof(context.Message.Document)),
                    MessageDataTestSupport.Require(await context.Message.Documents[0].Body.Value, nameof(context.Message.Documents)),
                    MessageDataTestSupport.Require(await context.Message.DocumentList[0].Body.Value, nameof(context.Message.DocumentList)),
                    MessageDataTestSupport.Require(await context.Message.DocumentIndex["first"].Body.Value, nameof(context.Message.DocumentIndex)),
                    MessageDataTestSupport.Require(await context.Message.DocumentIndex["second"].Body.Value, nameof(context.Message.DocumentIndex)));
            }));
        Guid identity = Guid.Parse("82d15d34-acde-4d71-9cf0-6155c3a534d2");
        byte[] identityBytes = identity.ToByteArray();
        MessageData<string> text = await repository.PutStringAsync(identity.ToString(), cancellationToken);
        MessageData<byte[]> bytes = await repository.PutBytesAsync(identityBytes, cancellationToken);
        await using var source = new MemoryStream(identityBytes, writable: false);
        MessageData<Stream> stream = await repository.PutStreamAsync(source, cancellationToken);
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

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            ReferenceSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);

            Assert.Equal(identity.ToString(), actual.Text);
            Assert.All(actual.ByteValues, value => Assert.Equal(identityBytes, value));
            Assert.Equal(7, actual.ByteValues.Count);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TOPOLOGY", "message-graph-without-data-has-no-transform")]
    public void MessageGraphWithoutMessageData_DoesNotCreateSendOrConsumeTransforms()
    {
        var repository = new InMemoryMessageDataRepository();
        var put = new PutMessageDataTransformSpecification<PlainEnvelope>(repository, MessageDataPolicy.Default);
        var get = new GetMessageDataTransformSpecification<PlainEnvelope>(repository);

        Assert.False(put.TryGetConverter(out _));
        Assert.False(put.TryGetSendTopology(out _));
        Assert.False(get.TryGetConverter(out _));
        Assert.False(get.TryGetConsumeTopology(out _));
    }

    private static InMemoryTestHarness CreateHarness(
        string prefix,
        TimeSpan timeout,
        IMessageDataRepository repository,
        MessageDataPolicy? policy = null)
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

    private static async Task ObserveAsync<T>(TaskCompletionSource<T> observed, Func<Task<T>> createSnapshot)
    {
        try
        {
            observed.TrySetResult(await createSnapshot());
        }
        catch (Exception exception)
        {
            observed.TrySetException(exception);
            throw;
        }
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

    public interface PolicyEnvelope
    {
        MessageData<string> Text { get; }
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
