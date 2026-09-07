using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Owns process-local inbox entries and grants exclusive access to each message attempt.</summary>
internal sealed class InMemoryOutboxMessageRepository :
    IDisposable
{
    readonly Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage> _dictionary;

    readonly SemaphoreSlim _dictionaryLock = new(1, 1);
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes an empty repository.</summary>
    /// <param name="timeProvider">The clock used to timestamp newly received messages.</param>
    public InMemoryOutboxMessageRepository(TimeProvider? timeProvider = null)
    {
        _dictionary = [];
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets or creates an inbox entry and waits for exclusive ownership of it.</summary>
    /// <param name="messageId">The identifier of the incoming message.</param>
    /// <param name="consumerId">The stable identifier of the logical consumer.</param>
    /// <param name="cancellationToken">The token that cancels either repository or entry lock acquisition.</param>
    /// <returns>A task containing the exclusively owned inbox entry.</returns>
    public async Task<InMemoryInboxMessage> LockAsync(Guid messageId, Guid consumerId, CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("The message identifier must not be empty.", nameof(messageId));
        if (consumerId == Guid.Empty)
            throw new ArgumentException("The consumer identifier must not be empty.", nameof(consumerId));

        var key = new InMemoryInboxMessageKey(messageId, consumerId);
        InMemoryInboxMessage existing;

        await _dictionaryLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_dictionary.TryGetValue(key, out existing!))
            {
                existing = new InMemoryInboxMessage(messageId, consumerId)
                {
                    Received = _timeProvider.GetUtcNow()
                };
                _dictionary.Add(key, existing);
            }
        }
        finally
        {
            _dictionaryLock.Release();
        }

        await existing.MarkInUseAsync(cancellationToken).ConfigureAwait(false);

        return existing;
    }

    /// <summary>Releases the synchronization resources owned by the repository and its entries.</summary>
    public void Dispose()
    {
        _dictionaryLock.Dispose();
        foreach (InMemoryInboxMessage message in _dictionary.Values)
            message.Dispose();
    }
}
