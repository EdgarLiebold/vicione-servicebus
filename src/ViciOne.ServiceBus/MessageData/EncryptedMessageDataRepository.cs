using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData;

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

    public async Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var cryptoStream = _streamProvider.GetEncryptStream(stream, null, CryptoStreamMode.Read);

        return await _repository.PutAsync(cryptoStream, timeToLive, cancellationToken).ConfigureAwait(false);
    }
}
