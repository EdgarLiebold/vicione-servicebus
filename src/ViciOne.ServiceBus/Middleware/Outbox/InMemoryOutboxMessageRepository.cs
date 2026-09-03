#nullable enable annotations
namespace ViciOne.ServiceBus.Middleware.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;


    public class InMemoryOutboxMessageRepository
    {
        readonly Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage> _dictionary;

        readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
        readonly TimeProvider _timeProvider;

        public InMemoryOutboxMessageRepository(TimeProvider? timeProvider = null)
        {
            _dictionary = new Dictionary<InMemoryInboxMessageKey, InMemoryInboxMessage>(InMemoryInboxMessageKey.Comparer);
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public Task MarkInUse(CancellationToken cancellationToken)
        {
            return _inUse.WaitAsync(cancellationToken);
        }

        public async Task<InMemoryInboxMessage> Lock(Guid messageId, Guid consumerId, CancellationToken cancellationToken)
        {
            var key = new InMemoryInboxMessageKey(messageId, consumerId);

            var existing = _dictionary.GetOrAdd(key, _ => new InMemoryInboxMessage(messageId, consumerId)
            {
                Received = _timeProvider.GetUtcNow().UtcDateTime,
                ReceiveCount = 0
            });

            await existing.MarkInUse(cancellationToken).ConfigureAwait(false);

            return existing;
        }

        public void Release()
        {
            _inUse.Release();
        }
    }
}
