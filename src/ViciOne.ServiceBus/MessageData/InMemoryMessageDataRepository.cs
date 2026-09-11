using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.MessageData;

/// <summary>Stores message data in process memory until its configured retention period expires.</summary>
public sealed class InMemoryMessageDataRepository :
    IMessageDataRepository
{
    readonly TimeProvider _timeProvider;
    readonly ConcurrentDictionary<Uri, Entry> _values;

    /// <summary>Creates an empty repository that evaluates retention against the supplied time source.</summary>
    /// <param name="timeProvider">The time source used to determine data expiration.</param>
    public InMemoryMessageDataRepository(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _values = new ConcurrentDictionary<Uri, Entry>();
    }

    Task<Stream> IMessageDataRepository.GetAsync(Uri address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        if (_values.TryGetValue(address, out Entry? entry))
        {
            if (!entry.ExpiresAt.HasValue || _timeProvider.GetUtcNow() < entry.ExpiresAt.Value)
                return Task.FromResult<Stream>(new MemoryStream(entry.Value, writable: false));

            _values.TryRemove(new KeyValuePair<Uri, Entry>(address, entry));
        }

        throw new MessageDataNotFoundException(address);
    }

    async Task<Uri> IMessageDataRepository.PutAsync(Stream stream, TimeSpan? timeToLive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        DateTimeOffset? expiresAt = GetExpiration(timeToLive);
        cancellationToken.ThrowIfCancellationRequested();

        Uri address = CreateAddress();

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

        if (!_values.TryAdd(address, new Entry(ms.ToArray(), expiresAt)))
            throw new InvalidOperationException($"The generated message-data address is already in use: {address}");

        return address;
    }

    DateTimeOffset? GetExpiration(TimeSpan? timeToLive)
    {
        if (!timeToLive.HasValue || timeToLive.Value == TimeSpan.MaxValue)
            return null;
        if (timeToLive.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "The retention period cannot be negative.");

        try
        {
            return _timeProvider.GetUtcNow().Add(timeToLive.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), timeToLive, "The retention period exceeds the supported UTC range.");
        }
    }

    static Uri CreateAddress()
    {
        NewId id = NewId.Next();
        return new Uri("urn:msgdata:" + FormatUtil.Formatter.Format(id.ToByteArray()));
    }

    sealed record Entry(byte[] Value, DateTimeOffset? ExpiresAt);
}
