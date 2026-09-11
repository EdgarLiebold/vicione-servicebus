using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>Encrypts message data before delegating storage to another repository.</summary>
public sealed class EncryptedMessageDataRepository :
    IMessageDataRepository
{
    readonly IMessageDataRepository _repository;
    readonly IEncryptionKeyProvider _keyProvider;
    readonly int _maximumObjectBytes;

    /// <summary>Creates an authenticated-encryption boundary over an inner repository.</summary>
    /// <param name="repository">The repository that stores encrypted envelopes.</param>
    /// <param name="keyProvider">The provider that selects current and historical encryption keys.</param>
    /// <param name="maximumObjectBytes">The hard upper bound for one plaintext message-data object.</param>
    public EncryptedMessageDataRepository(
        IMessageDataRepository repository,
        IEncryptionKeyProvider keyProvider,
        int maximumObjectBytes)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumObjectBytes, 1);
        if (maximumObjectBytes > int.MaxValue - ushort.MaxValue - 64)
            throw new ArgumentOutOfRangeException(nameof(maximumObjectBytes), "The encrypted-object limit is too large for a bounded envelope.");

        _repository = repository;
        _keyProvider = keyProvider;
        _maximumObjectBytes = maximumObjectBytes;
    }

    /// <summary>Loads and authenticates one encrypted envelope, returning its plaintext stream.</summary>
    /// <param name="address">The inner repository address.</param>
    /// <param name="cancellationToken">The token that cancels retrieval and decryption.</param>
    /// <returns>A plaintext stream owned by the caller.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        await using Stream stream = await _repository.GetAsync(address, cancellationToken).ConfigureAwait(false);
        return await AesGcmMessageDataEncryption
            .DecryptAsync(stream, _keyProvider, _maximumObjectBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Encrypts the remaining plaintext stream and stores the resulting envelope.</summary>
    /// <param name="stream">The caller-owned plaintext stream.</param>
    /// <param name="timeToLive">The optional retention period forwarded to the inner repository.</param>
    /// <param name="cancellationToken">The token that cancels encryption and storage.</param>
    /// <returns>The address assigned by the inner repository.</returns>
    public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (timeToLive is { } retention && retention < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "The retention period cannot be negative.");
        cancellationToken.ThrowIfCancellationRequested();

        await using Stream encrypted = await AesGcmMessageDataEncryption
            .EncryptAsync(stream, _keyProvider, _maximumObjectBytes, cancellationToken)
            .ConfigureAwait(false);
        return await _repository.PutAsync(encrypted, timeToLive, cancellationToken).ConfigureAwait(false);
    }
}
