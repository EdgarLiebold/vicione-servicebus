using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>
/// Provides an in memory message data repository implementation.
/// </summary>
public class InMemoryMessageDataRepository :
    IMessageDataRepository
{
    readonly ConcurrentDictionary<Uri, byte[]> _values;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InMemoryMessageDataRepository()
    {
        _values = new ConcurrentDictionary<Uri, byte[]>();
    }

    Task<Stream> IMessageDataRepository.GetAsync(Uri address, CancellationToken cancellationToken)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        if (_values.TryGetValue(address, out var value))
            return Task.FromResult<Stream>(new MemoryStream(value, false));

        throw new MessageDataNotFoundException(address);
    }

    async Task<Uri> IMessageDataRepository.PutAsync(Stream stream, TimeSpan? timeToLive, CancellationToken cancellationToken)
    {
        var address = new InMemoryMessageDataId().Uri;

        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms).ConfigureAwait(false);

        _values.TryAdd(address, ms.ToArray());

        return address;
    }
}
