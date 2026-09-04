using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

#nullable enable annotations
namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an in memory outbox message repository implementation.
/// </summary>
public class InMemoryOutboxMessageRepository
{
    readonly Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage> _dictionary;

    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    public InMemoryOutboxMessageRepository(TimeProvider? timeProvider = null)
    {
        _dictionary = new Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage>(InMemoryInboxMessageKey.Comparer);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Performs the mark in use operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the lock operation.
    /// </summary>
    /// <param name="messageId">The message id value.</param>
    /// <param name="consumerId">The consumer id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    public void Release()
    {
        _inUse.Release();
    }
}
