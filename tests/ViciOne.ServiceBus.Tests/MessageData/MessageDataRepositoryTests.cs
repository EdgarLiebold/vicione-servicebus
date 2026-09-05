using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataRepositoryTests
{
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
            var address = new Uri($"urn:recorded:{NewId.NextGuid():N}");
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            Assert.True(_values.TryAdd(address, copy.ToArray()));
            Interlocked.Increment(ref _putCalls);
            return address;
        }

        public byte[] StoredBytes(Uri address) => _values[address];
    }

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
