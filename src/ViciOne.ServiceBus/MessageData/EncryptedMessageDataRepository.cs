using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>
/// Provides an encrypted message data repository implementation.
/// </summary>
public class EncryptedMessageDataRepository :
    IMessageDataRepository
{
    readonly IMessageDataRepository _repository;
    readonly ICryptoStreamProvider _streamProvider;

    /// <summary>
    /// Provides encrypted stream support to ensure that message data is encrypted at rest.
    /// </summary>
    /// <param name="repository">The original message data repository where message data is stored.</param>
    /// <param name="streamProvider">The encrypted stream provider</param>
    public EncryptedMessageDataRepository(IMessageDataRepository repository, ICryptoStreamProvider streamProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(streamProvider);

        _repository = repository;
        _streamProvider = streamProvider;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        var stream = await _repository.GetAsync(address, cancellationToken).ConfigureAwait(false);

        try
        {
            return _streamProvider.GetDecryptStream(stream, null, CryptoStreamMode.Read);
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Performs the put operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var cryptoStream = _streamProvider.GetEncryptStream(stream, null, CryptoStreamMode.Read);

        return await _repository.PutAsync(cryptoStream, timeToLive, cancellationToken).ConfigureAwait(false);
    }
}
