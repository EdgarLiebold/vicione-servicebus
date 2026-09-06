using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Stores and retrieves in memory outbox message data.</summary>
public class InMemoryOutboxMessageRepository
{
    readonly Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage> _dictionary;

    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public InMemoryOutboxMessageRepository(TimeProvider? timeProvider = null)
    {
        _dictionary = new Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage>(InMemoryInboxMessageKey.Comparer);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Marks in use.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>Acquires the configured lock.</summary>
    /// <param name="messageId">The message id.</param>
    /// <param name="consumerId">The consumer id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the lock outcome.</returns>
    public async Task<InMemoryInboxMessage> LockAsync(Guid messageId, Guid consumerId, CancellationToken cancellationToken)
    {
        var key = new InMemoryInboxMessageKey(messageId, consumerId);

        var existing = _dictionary.GetOrAdd(key, _ => new InMemoryInboxMessage(messageId, consumerId)
        {
            Received = _timeProvider.GetUtcNow().UtcDateTime,
            ReceiveCount = 0
        });

        await existing.MarkInUseAsync(cancellationToken).ConfigureAwait(false);

        return existing;
    }

    /// <summary>Releases the owned resource.</summary>
    public void Release()
    {
        _inUse.Release();
    }
}
