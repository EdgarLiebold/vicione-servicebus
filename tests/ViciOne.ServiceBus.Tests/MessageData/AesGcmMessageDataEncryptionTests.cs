using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class AesGcmMessageDataEncryptionTests
{
    private const int HeaderPrefixLength = 7;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(1024)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "boundary-length-round-trips")]
    public async Task BoundaryLengths_RoundTripWithoutBlockSizeAssumptionsAsync(int length)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var repository = new EncryptedMessageDataRepository(inner, new TestEncryptionKeyProvider(), 1024);
        byte[] expected = Enumerable.Range(0, length).Select(static index => (byte)(index % 251)).ToArray();

        Uri address = await PutAsync(repository, expected, cancellationToken);

        Assert.Equal(expected, await GetAsync(repository, address, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "versioned-envelope-round-trip")]
    public async Task PutAndGet_WriteTheVersionedEnvelopeAndRoundTripExactBytesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var repository = new EncryptedMessageDataRepository(inner, new TestEncryptionKeyProvider("primary", 11), 1024);
        byte[] plaintext = Encoding.UTF8.GetBytes("authenticated message data");

        Uri address = await PutAsync(repository, plaintext, cancellationToken);
        byte[] envelope = inner.GetBytes(address);
        byte[] restored = await GetAsync(repository, address, cancellationToken);

        Assert.Equal("VOSB"u8.ToArray(), envelope[..4]);
        Assert.Equal(1, envelope[4]);
        Assert.Equal("primary", ReadKeyId(envelope));
        Assert.Equal(HeaderPrefixLength + "primary"u8.Length + NonceLength + plaintext.Length + TagLength, envelope.Length);
        Assert.DoesNotContain(Encoding.UTF8.GetString(plaintext), Encoding.UTF8.GetString(envelope), StringComparison.Ordinal);
        Assert.Equal(plaintext, restored);
    }

    [Theory]
    [InlineData(TamperedRegion.KeyId)]
    [InlineData(TamperedRegion.Nonce)]
    [InlineData(TamperedRegion.Ciphertext)]
    [InlineData(TamperedRegion.Tag)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "authenticated-regions-fail-closed")]
    public async Task Get_RejectsTamperingInEveryAuthenticatedRegionAsync(TamperedRegion region)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var keys = new TestEncryptionKeyProvider();
        var repository = new EncryptedMessageDataRepository(inner, keys, 1024);
        Uri address = await PutAsync(repository, [10, 20, 30, 40], cancellationToken);
        byte[] envelope = inner.GetBytes(address);
        int keyIdLength = BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
        int nonceOffset = HeaderPrefixLength + keyIdLength;
        int index = region switch
        {
            TamperedRegion.KeyId => HeaderPrefixLength,
            TamperedRegion.Nonce => nonceOffset,
            TamperedRegion.Ciphertext => nonceOffset + NonceLength,
            TamperedRegion.Tag => envelope.Length - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(region)),
        };
        envelope[index] ^= 0x40;
        inner.Replace(address, envelope);

        SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() =>
            repository.GetAsync(address, cancellationToken));

        Assert.True(
            exception.Message == "Encrypted message data authentication failed."
            || exception.Message.StartsWith("Encryption key '", StringComparison.Ordinal)
            && exception.Message.EndsWith("' was not found.", StringComparison.Ordinal),
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "wrong-key-fails-closed")]
    public async Task Get_WithDifferentMaterialUnderTheSameKeyId_FailsAuthenticationAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        Uri address = await PutAsync(
            new EncryptedMessageDataRepository(inner, new TestEncryptionKeyProvider("shared", 1), 1024),
            [1, 3, 5, 7],
            cancellationToken);
        var wrongKeyRepository = new EncryptedMessageDataRepository(inner, new TestEncryptionKeyProvider("shared", 91), 1024);

        SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() =>
            wrongKeyRepository.GetAsync(address, cancellationToken));

        Assert.Equal("Encrypted message data authentication failed.", exception.Message);
        Assert.IsType<System.Security.Cryptography.AuthenticationTagMismatchException>(exception.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "key-rotation-retains-historical-decryption")]
    public async Task Rotation_UsesTheNewKeyForWritesAndRetainsHistoricalReadsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var keys = new TestEncryptionKeyProvider("2026-01", 1);
        var repository = new EncryptedMessageDataRepository(inner, keys, 1024);
        Uri oldAddress = await PutAsync(repository, [1, 2, 3], cancellationToken);

        keys.Rotate("2026-09", 51);
        Uri newAddress = await PutAsync(repository, [4, 5, 6], cancellationToken);

        Assert.Equal("2026-01", ReadKeyId(inner.GetBytes(oldAddress)));
        Assert.Equal("2026-09", ReadKeyId(inner.GetBytes(newAddress)));
        Assert.Equal([1, 2, 3], await GetAsync(repository, oldAddress, cancellationToken));
        Assert.Equal([4, 5, 6], await GetAsync(repository, newAddress, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "removed-rotation-key-fails-closed")]
    public async Task Get_AfterHistoricalKeyRemoval_FailsWithoutReturningCiphertextAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var keys = new TestEncryptionKeyProvider("retired", 1);
        var repository = new EncryptedMessageDataRepository(inner, keys, 1024);
        Uri address = await PutAsync(repository, [1, 2, 3], cancellationToken);
        keys.Rotate("active", 51, retainPrevious: false);

        SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() =>
            repository.GetAsync(address, cancellationToken));

        Assert.Equal("Encryption key 'retired' was not found.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "ten-thousand-random-nonces-are-unique")]
    public async Task TenThousandObjects_UseUniqueNinetySixBitNoncesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var repository = new EncryptedMessageDataRepository(inner, new TestEncryptionKeyProvider(), 1024);
        var nonces = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < 10_000; index++)
        {
            Uri address = await PutAsync(repository, BitConverter.GetBytes(index), cancellationToken);
            byte[] envelope = inner.GetBytes(address);
            int nonceOffset = HeaderPrefixLength + BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
            Assert.True(nonces.Add(Convert.ToHexString(envelope.AsSpan(nonceOffset, NonceLength))), $"Duplicate nonce at object {index}.");
        }

        Assert.Equal(10_000, nonces.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "bounded-input-and-decrypted-length")]
    public async Task ObjectLimit_RejectsBeforeStorageAndBeforePlaintextExposureAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var inner = new MutableRepository();
        var keys = new TestEncryptionKeyProvider();
        var bounded = new EncryptedMessageDataRepository(inner, keys, 64);
        byte[] oversized = new byte[65];

        PayloadAdmissionException writeFailure = await Assert.ThrowsAsync<PayloadAdmissionException>(() =>
            PutAsync(bounded, oversized, cancellationToken));

        Assert.Equal(PayloadAdmissionStage.MessageData, writeFailure.Stage);
        Assert.Equal(65, writeFailure.ActualBytes);
        Assert.Equal(64, writeFailure.ConfiguredLimitBytes);
        Assert.Equal(0, inner.Count);

        var writer = new EncryptedMessageDataRepository(inner, keys, 65);
        Uri address = await PutAsync(writer, oversized, cancellationToken);

        PayloadAdmissionException readFailure = await Assert.ThrowsAsync<PayloadAdmissionException>(() =>
            bounded.GetAsync(address, cancellationToken));

        Assert.Equal(PayloadAdmissionStage.MessageData, readFailure.Stage);
        Assert.Equal(65, readFailure.ActualBytes);
        Assert.Equal(64, readFailure.ConfiguredLimitBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(33)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "aes-key-size-validation")]
    public void EncryptionKey_RejectsUnsupportedAesKeySizes(int length)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EncryptionKey("key", new byte[length]));

        Assert.Equal("keyMaterial", exception.ParamName);
        Assert.StartsWith("AES key material must contain 16, 24, or 32 bytes.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "key-material-defensive-copy")]
    public void EncryptionKey_TakesAndReturnsDefensiveCopies()
    {
        byte[] source = Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray();
        var key = new EncryptionKey("key", source);
        source[0] = 0;
        byte[] firstExport = key.ExportKeyMaterial();
        firstExport[1] = 0;

        byte[] secondExport = key.ExportKeyMaterial();

        Assert.Equal(1, secondExport[0]);
        Assert.Equal(2, secondExport[1]);
        Assert.NotSame(firstExport, secondExport);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-AES-GCM", "key-identifier-envelope-bound")]
    public void EncryptionKey_RejectsAnIdentifierThatCannotFitTheEnvelope()
    {
        string oversizedKeyId = new('€', 21_846);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new EncryptionKey(oversizedKeyId, new byte[32]));

        Assert.Equal("keyId", exception.ParamName);
        Assert.StartsWith("The key identifier cannot exceed 65535 UTF-8 bytes.", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<byte[]> GetAsync(
        EncryptedMessageDataRepository repository,
        Uri address,
        CancellationToken cancellationToken)
    {
        await using Stream stream = await repository.GetAsync(address, cancellationToken);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static async Task<Uri> PutAsync(
        EncryptedMessageDataRepository repository,
        byte[] value,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(value, writable: false);
        return await repository.PutAsync(stream, cancellationToken: cancellationToken);
    }

    private static string ReadKeyId(byte[] envelope)
    {
        int length = BinaryPrimitives.ReadUInt16BigEndian(envelope.AsSpan(5, 2));
        return Encoding.UTF8.GetString(envelope, HeaderPrefixLength, length);
    }

    public enum TamperedRegion
    {
        KeyId,
        Nonce,
        Ciphertext,
        Tag,
    }

    private sealed class MutableRepository : IMessageDataRepository
    {
        private readonly ConcurrentDictionary<Uri, byte[]> _values = new();
        private long _sequence;

        public int Count => _values.Count;

        public Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_values.TryGetValue(address, out byte[]? value))
                throw new MessageDataNotFoundException(address);

            return Task.FromResult<Stream>(new MemoryStream(value, writable: false));
        }

        public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            var address = new Uri($"urn:aes-gcm:{Interlocked.Increment(ref _sequence)}");
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            Assert.True(_values.TryAdd(address, buffer.ToArray()));
            return address;
        }

        public byte[] GetBytes(Uri address) => _values[address].ToArray();

        public void Replace(Uri address, byte[] value) => _values[address] = value.ToArray();
    }
}
