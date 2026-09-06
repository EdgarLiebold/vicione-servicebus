using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>Stores and retrieves encrypted message data data.</summary>
public class EncryptedMessageDataRepository :
    IMessageDataRepository
{
    readonly IMessageDataRepository _repository;
    readonly IEncryptionKeyProvider _keyProvider;
    readonly int _maximumObjectBytes;

    /// <summary>Provides encrypted stream support to ensure that message data is encrypted at rest.</summary>
    /// <param name="repository">The original message data repository where message data is stored.</param>
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

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        await using Stream stream = await _repository.GetAsync(address, cancellationToken).ConfigureAwait(false);
        return await AesGcmMessageDataEncryption
            .DecryptAsync(stream, _keyProvider, _maximumObjectBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Writes the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put outcome.</returns>
    public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await using Stream encrypted = await AesGcmMessageDataEncryption
            .EncryptAsync(stream, _keyProvider, _maximumObjectBytes, cancellationToken)
            .ConfigureAwait(false);
        return await _repository.PutAsync(encrypted, timeToLive, cancellationToken).ConfigureAwait(false);
    }
}
