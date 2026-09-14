using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataRepositoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "null-values-produce-empty-data")]
    public async Task NullValues_ProduceEmptyMessageDataWithoutRepositoryAccessAsync()
    {
        var repository = new RecordingRepository();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        MessageData<string> text = await repository.PutStringAsync(null, cancellationToken);
        MessageData<byte[]> bytes = await repository.PutBytesAsync(null, cancellationToken);
        IMessageData value = await repository.PutObjectAsync(null, typeof(object), cancellationToken);
        MessageData<Stream> stream = await repository.PutStreamAsync(null, cancellationToken);

        Assert.False(text.HasValue);
        Assert.False(bytes.HasValue);
        Assert.False(value.HasValue);
        Assert.False(stream.HasValue);
        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "inline-and-empty-paths-own-cancellation")]
    public async Task InlineAndEmptyPaths_ObservePreCanceledOperationsAsync()
    {
        var repository = new RecordingRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.PutStringAsync("inline", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.PutBytesAsync([1], cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.PutObjectAsync(new object(), typeof(object), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.PutStreamAsync(null, cancellation.Token));

        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "byte-input-is-snapshotted")]
    public async Task ByteStorage_SnapshotsCallerOwnedArraysForInlineAndStoredResultsAsync()
    {
        var repository = new RecordingRepository();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] inlineInput = [1, 2, 3];
        byte[] storedInput = [4, 5, 6];
        var inlinePolicy = new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 16);
        var storedPolicy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);

        MessageData<byte[]> inline = await repository.PutBytesAsync(inlineInput, null, inlinePolicy, cancellationToken);
        MessageData<byte[]> stored = await repository.PutBytesAsync(storedInput, null, storedPolicy, cancellationToken);
        inlineInput[0] = 99;
        storedInput[0] = 99;

        Assert.Equal([1, 2, 3], await inline.Value);
        Assert.Equal([4, 5, 6], await stored.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "object-type-and-read-address-boundaries")]
    public async Task ObjectAndReadOperations_RejectNullContractTypesAndAddressesAsync()
    {
        var repository = new RecordingRepository();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("objectType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.PutObjectAsync(new object(), null!, cancellationToken))).ParamName);
        Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.GetStringAsync(null!, cancellationToken))).ParamName);
        Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.GetBytesAsync(null!, cancellationToken))).ParamName);
        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "explicit-policy-null-boundary")]
    public async Task ExplicitPolicyOverloads_RejectANullPolicyBeforeUsingTheValueAsync()
    {
        var repository = new RecordingRepository();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("policy", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.PutStringAsync("value", null, null!, cancellationToken))).ParamName);
        Assert.Equal("policy", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.PutBytesAsync([1], null, null!, cancellationToken))).ParamName);
        Assert.Equal("policy", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.PutObjectAsync(new object(), typeof(object), null, null!, cancellationToken))).ParamName);
        Assert.Equal(0, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-POLICY", "retention-overloads-forward-exact-value-lifetime-and-cancellation")]
    public async Task RetentionOverloads_ForwardExactValuesLifetimeAndCancellationAsync()
    {
        var repository = new RecordingRepository();
        var timeToLive = TimeSpan.FromMinutes(43);
        using var cancellation = new CancellationTokenSource();
        const string text = "retained-text";
        byte[] bytes = [2, 3, 5, 7, 11];
        var payload = new RetainedPayload("north", 17);

        MessageData<string> textData = await repository.PutStringAsync(text, timeToLive, cancellation.Token);
        Assert.Equal(timeToLive, repository.LastTimeToLive);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        Assert.Equal(Encoding.UTF8.GetBytes(text), repository.StoredBytes(MessageDataTestSupport.Require(textData.Address, nameof(textData))));

        MessageData<byte[]> byteData = await repository.PutBytesAsync(bytes, timeToLive, cancellation.Token);
        Assert.Equal(timeToLive, repository.LastTimeToLive);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        Assert.Equal(bytes, repository.StoredBytes(MessageDataTestSupport.Require(byteData.Address, nameof(byteData))));

        IMessageData objectData = await repository.PutObjectAsync(payload, typeof(RetainedPayload), timeToLive, cancellation.Token);
        Assert.Equal(timeToLive, repository.LastTimeToLive);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        byte[] serialized = repository.StoredBytes(MessageDataTestSupport.Require(objectData.Address, nameof(objectData)));
        Assert.Equal(payload, JsonSerializer.Deserialize<RetainedPayload>(serialized, ServiceBusMetadataJson.Options));
        Assert.Equal(3, repository.PutCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORY", "in-memory-exact-round-trip-and-missing-address")]
    public async Task InMemoryRepository_RoundTripsExactBytesAndRejectsAnUnknownAddressAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMessageDataRepository repository = new InMemoryMessageDataRepository();
        byte[] expected = Enumerable.Range(0, 257).Select(index => (byte)(index % 251)).ToArray();
        Uri address;
        await using (var source = new MemoryStream(expected, writable: false))
            address = await repository.PutAsync(source, cancellationToken: cancellationToken);

        await using Stream stored = await repository.GetAsync(address, cancellationToken);
        using var copy = new MemoryStream();
        await stored.CopyToAsync(copy, cancellationToken);

        Assert.Equal("urn", address.Scheme);
        Assert.StartsWith("urn:msgdata:", address.OriginalString, StringComparison.Ordinal);
        Assert.Equal(expected, copy.ToArray());

        Uri missing = new("urn:msgdata:missing");
        MessageDataNotFoundException exception = await Assert.ThrowsAsync<MessageDataNotFoundException>(
            () => repository.GetAsync(missing, cancellationToken));
        Assert.Equal($"The message data was not found: {missing}", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORIES", "required-arguments-cancellation-and-retention-boundaries")]
    public async Task RepositoryImplementations_ValidateBeforeAllocatingOrPerformingIoAsync()
    {
        var inMemory = (IMessageDataRepository)new InMemoryMessageDataRepository();
        string root = Path.Combine(Path.GetTempPath(), "vsb-message-data-boundaries", NewId.NextGuid().ToString("N"));
        var directory = new DirectoryInfo(root);
        var fileSystem = (IMessageDataRepository)new FileSystemMessageDataRepository(directory);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Equal("dataDirectory", Assert.Throws<ArgumentNullException>(() =>
            new FileSystemMessageDataRepository(null!)).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            inMemory.PutAsync(null!, cancellationToken: CancellationToken.None))).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fileSystem.PutAsync(null!, cancellationToken: CancellationToken.None))).ParamName);

        OperationCanceledException inMemoryGet = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            inMemory.GetAsync(new Uri("urn:msgdata:missing"), cancellation.Token));
        OperationCanceledException fileGet = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fileSystem.GetAsync(new Uri("urn:file:none:missing"), cancellation.Token));
        Assert.Equal(cancellation.Token, inMemoryGet.CancellationToken);
        Assert.Equal(cancellation.Token, fileGet.CancellationToken);

        await using var inMemorySource = new MemoryStream([1], writable: false);
        await using var fileSource = new MemoryStream([2], writable: false);
        Assert.Equal("timeToLive", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            inMemory.PutAsync(inMemorySource, TimeSpan.FromTicks(-1), CancellationToken.None))).ParamName);
        Assert.Equal("timeToLive", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            fileSystem.PutAsync(fileSource, TimeSpan.FromTicks(-1), CancellationToken.None))).ParamName);

        await using var canceledSource = new MemoryStream([3], writable: false);
        OperationCanceledException filePut = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fileSystem.PutAsync(canceledSource, cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, filePut.CancellationToken);
        Assert.False(directory.Exists);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORIES", "in-memory-exact-expiration-boundary")]
    public async Task InMemoryRepository_ExpiresDataAtTheExactRetentionBoundaryAsync()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        IMessageDataRepository repository = new InMemoryMessageDataRepository(clock);
        await using var source = new MemoryStream([7, 8, 9], writable: false);
        Uri address = await repository.PutAsync(source, TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromSeconds(10) - TimeSpan.FromTicks(1));
        await using (Stream value = await repository.GetAsync(address, TestContext.Current.CancellationToken))
        {
            using var copy = new MemoryStream();
            await value.CopyToAsync(copy, TestContext.Current.CancellationToken);
            Assert.Equal([7, 8, 9], copy.ToArray());
        }

        clock.Advance(TimeSpan.FromTicks(1));
        MessageDataNotFoundException exception = await Assert.ThrowsAsync<MessageDataNotFoundException>(() =>
            repository.GetAsync(address, TestContext.Current.CancellationToken));
        Assert.Equal(address, exception.Address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORIES", "filesystem-exact-expiration-and-path-containment")]
    public async Task FileSystemRepository_ExpiresPreciselyAndRejectsPathsOutsideItsRootAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "vsb-message-data-containment", NewId.NextGuid().ToString("N"));
        var directory = new DirectoryInfo(root);
        var clock = new FakeTimeProvider(new DateTimeOffset(2043, 3, 4, 5, 6, 7, TimeSpan.Zero));
        IMessageDataRepository repository = new FileSystemMessageDataRepository(directory, clock);

        try
        {
            Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.GetAsync(new Uri("urn:file:..:outside"), TestContext.Current.CancellationToken))).ParamName);
            Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.GetAsync(new Uri("urn:other:value"), TestContext.Current.CancellationToken))).ParamName);

            await using var source = new MemoryStream([10, 11], writable: false);
            Uri address = await repository.PutAsync(source, TimeSpan.FromMinutes(15), TestContext.Current.CancellationToken);
            FileInfo file = Assert.Single(directory.EnumerateFiles("*", SearchOption.AllDirectories));

            clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
            await using (Stream value = await repository.GetAsync(address, TestContext.Current.CancellationToken))
            {
                using var copy = new MemoryStream();
                await value.CopyToAsync(copy, TestContext.Current.CancellationToken);
                Assert.Equal([10, 11], copy.ToArray());
            }

            clock.Advance(TimeSpan.FromTicks(1));
            MessageDataNotFoundException exception = await Assert.ThrowsAsync<MessageDataNotFoundException>(() =>
                repository.GetAsync(address, TestContext.Current.CancellationToken));
            Assert.Equal(address, exception.Address);
            file.Refresh();
            Assert.False(file.Exists);
        }
        finally
        {
            if (directory.Exists)
                directory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REPOSITORY", "filesystem-permanent-and-expiring-paths")]
    public async Task FileSystemRepository_RoundTripsOneExactFileInTheExpectedPathClassAsync(bool expiring)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string root = Path.Combine(Path.GetTempPath(), "vsb-message-data", NewId.NextGuid().ToString("N"));
        var directory = new DirectoryInfo(root);
        IMessageDataRepository repository = new FileSystemMessageDataRepository(directory);
        byte[] expected = Encoding.UTF8.GetBytes($"filesystem-{NewId.NextGuid():N}");

        try
        {
            Uri address;
            await using (var source = new MemoryStream(expected, writable: false))
                address = await repository.PutAsync(source, expiring ? TimeSpan.FromDays(30) : null, cancellationToken);

            await using Stream stored = await repository.GetAsync(address, cancellationToken);
            using var copy = new MemoryStream();
            await stored.CopyToAsync(copy, cancellationToken);

            Assert.Equal(expected, copy.ToArray());
            Assert.Equal(expiring, !address.OriginalString.Contains(":none:", StringComparison.Ordinal));
            FileInfo file = Assert.Single(directory.EnumerateFiles("*", SearchOption.AllDirectories));
            Assert.Equal(expected.Length, file.Length);
        }
        finally
        {
            if (directory.Exists)
                directory.Delete(recursive: true);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-TTL-CLOCK", "filesystem-expiration-path")]
    public async Task FileSystemExpirationPath_UsesTheInjectedClockAndExactTimeToLiveAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string root = Path.Combine(Path.GetTempPath(), "vsb-message-data-clock", NewId.NextGuid().ToString("N"));
        var directory = new DirectoryInfo(root);
        var clock = new FakeTimeProvider(new DateTimeOffset(2041, 12, 31, 23, 30, 0, TimeSpan.Zero));
        IMessageDataRepository repository = new FileSystemMessageDataRepository(directory, clock);

        try
        {
            await using var source = new MemoryStream([1, 2, 3], writable: false);

            Uri address = await repository.PutAsync(source, TimeSpan.FromMinutes(90), cancellationToken);

            Assert.StartsWith("urn:file:2042:01:01:01:", address.OriginalString, StringComparison.Ordinal);
            Assert.Single(directory.EnumerateFiles("*", SearchOption.AllDirectories));
        }
        finally
        {
            if (directory.Exists)
                directory.Delete(recursive: true);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-THRESHOLD", "string-and-bytes-inline-stream-stored")]
    public async Task InlineThreshold_EmbedsStringAndBytesButAlwaysStoresStreamsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var policy = new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 4096);
        var repository = new RecordingRepository();
        const string text = "inline-text";
        byte[] bytes = [1, 3, 5, 7];
        byte[] streamBytes = [2, 4, 6, 8];

        MessageData<string> stringData = await repository.PutStringAsync(text, null, policy, cancellationToken);
        MessageData<byte[]> byteData = await repository.PutBytesAsync(bytes, null, policy, cancellationToken);
        await using var source = new MemoryStream(streamBytes, writable: false);
        MessageData<Stream> streamData = await repository.PutStreamAsync(source, cancellationToken);

        Assert.Null(stringData.Address);
        Assert.Null(byteData.Address);
        Assert.Equal(text, await stringData.Value);
        Assert.Equal(bytes, await byteData.Value);
        Assert.NotNull(streamData.Address);
        Assert.Equal(1, repository.PutCalls);
        Assert.Equal(streamBytes, repository.StoredBytes(streamData.Address));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-ENCRYPTION", "ciphertext-at-rest-and-three-type-round-trip")]
    public async Task EncryptedRepository_StoresCiphertextAndRoundTripsStringBytesAndStreamAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var policy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);
        var inner = new RecordingRepository();
        var repository = new EncryptedMessageDataRepository(
            inner,
            new TestEncryptionKeyProvider(),
            1024 * 1024);
        string text = $"encrypted-{NewId.NextGuid():N}";
        byte[] bytes = Enumerable.Range(0, 129).Select(index => (byte)(index % 127)).ToArray();
        byte[] streamBytes = Enumerable.Range(0, 65).Select(index => (byte)(255 - index)).ToArray();

        MessageData<string> stringData = await repository.PutStringAsync(text, null, policy, cancellationToken);
        MessageData<byte[]> byteData = await repository.PutBytesAsync(bytes, null, policy, cancellationToken);
        await using var source = new MemoryStream(streamBytes, writable: false);
        MessageData<Stream> streamData = await repository.PutStreamAsync(source, cancellationToken);

        Assert.NotEqual(Encoding.UTF8.GetBytes(text), inner.StoredBytes(MessageDataTestSupport.Require(stringData.Address, nameof(stringData))));
        Assert.NotEqual(bytes, inner.StoredBytes(MessageDataTestSupport.Require(byteData.Address, nameof(byteData))));
        Assert.NotEqual(streamBytes, inner.StoredBytes(MessageDataTestSupport.Require(streamData.Address, nameof(streamData))));

        string restoredText = MessageDataTestSupport.Require(
            await (await repository.GetStringAsync(MessageDataTestSupport.Require(stringData.Address, nameof(stringData)), cancellationToken)).Value,
            nameof(stringData));
        Assert.Equal(text.Select(static character => (int)character), restoredText.Select(static character => (int)character));
        Assert.Equal(bytes, await (await repository.GetBytesAsync(
            MessageDataTestSupport.Require(byteData.Address, nameof(byteData)), cancellationToken)).Value);
        await using Stream restored = await repository.GetAsync(
            MessageDataTestSupport.Require(streamData.Address, nameof(streamData)), cancellationToken);
        using var copy = new MemoryStream();
        await restored.CopyToAsync(copy, cancellationToken);
        Assert.Equal(streamBytes, copy.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-ENCRYPTION", "public-null-boundaries")]
    public async Task EncryptedRepository_RejectsNullDependenciesAddressesAndStreamsAtThePublicBoundaryAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var keyProvider = new TestEncryptionKeyProvider();

        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            new EncryptedMessageDataRepository(null!, keyProvider, 1024)).ParamName);
        Assert.Equal("keyProvider", Assert.Throws<ArgumentNullException>(() =>
            new EncryptedMessageDataRepository(repository, null!, 1024)).ParamName);

        Assert.Equal("maximumObjectBytes", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncryptedMessageDataRepository(repository, keyProvider, 0)).ParamName);

        var encrypted = new EncryptedMessageDataRepository(repository, keyProvider, 1024);
        Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            encrypted.GetAsync(null!, cancellationToken))).ParamName);
        Assert.Equal("stream", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            encrypted.PutAsync(null!, cancellationToken: cancellationToken))).ParamName);
        Assert.Equal("timeToLive", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            encrypted.PutAsync(Stream.Null, TimeSpan.FromTicks(-1), cancellationToken))).ParamName);

        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            encrypted.GetAsync(new Uri("urn:encrypted:canceled"), canceled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            encrypted.PutAsync(Stream.Null, cancellationToken: canceled.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-ENCRYPTION", "invalid-envelope-releases-owned-stream")]
    public async Task InvalidEnvelope_DisposesTheOwnedInnerStreamAndFailsClosedAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var innerStream = new TrackingStream([1, 2, 3]);
        var repository = new SingleStreamRepository(innerStream);
        var encrypted = new EncryptedMessageDataRepository(repository, new TestEncryptionKeyProvider(), 1024);

        SerializationException actual = await Assert.ThrowsAsync<SerializationException>(() =>
            encrypted.GetAsync(new Uri("urn:encrypted:test"), cancellationToken));

        Assert.Equal("Encrypted message data envelope is invalid. The envelope is truncated.", actual.Message);
        Assert.True(innerStream.IsDisposed);
        Assert.Equal(1, repository.GetCalls);
    }

    private sealed class RecordingRepository : IMessageDataRepository
    {
        private readonly ConcurrentDictionary<Uri, byte[]> _values = new();
        private int _putCalls;

        public int PutCalls => Volatile.Read(ref _putCalls);

        public TimeSpan? LastTimeToLive { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_values.TryGetValue(address, out byte[]? value))
                throw new MessageDataNotFoundException(address);

            return Task.FromResult<Stream>(new MemoryStream(value, writable: false));
        }

        public async Task<Uri> PutAsync(
            Stream stream,
            TimeSpan? timeToLive = null,
            CancellationToken cancellationToken = default)
        {
            LastTimeToLive = timeToLive;
            LastCancellationToken = cancellationToken;
            var address = new Uri($"urn:recorded:{NewId.NextGuid():N}");
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            Assert.True(_values.TryAdd(address, copy.ToArray()));
            Interlocked.Increment(ref _putCalls);
            return address;
        }

        public byte[] StoredBytes(Uri address) => _values[address];
    }

    private sealed record RetainedPayload(string Tenant, int Attempt);

    private sealed class SingleStreamRepository(TrackingStream stream) : IMessageDataRepository
    {
        private int _getCalls;

        public int GetCalls => Volatile.Read(ref _getCalls);

        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _getCalls);
            return Task.FromResult<Stream>(stream);
        }

        public Task<Uri> PutAsync(Stream value, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Uri>(cancellationToken); throw new NotSupportedException(); }
    }

    private sealed class TrackingStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
