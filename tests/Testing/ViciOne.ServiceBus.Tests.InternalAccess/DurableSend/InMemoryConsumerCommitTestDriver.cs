namespace ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;

/// <summary>Exposes the in-memory inbox/outbox commit boundary to behavioral tests.</summary>
public sealed class InMemoryConsumerCommitTestDriver<TBus>
    where TBus : class, IBus
{
    private readonly InMemoryReliableStore<TBus> _store = new();

    public IInboxStore<TBus> Inbox => _store;

    public IOutboxStore<TBus> Outbox => _store;

    public void CompleteConsumer(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        IReadOnlyList<SerializedDurableSend> messages,
        DurableSendStoreLimits limits,
        DateTimeOffset consumedAt)
        => _store.CompleteConsumer(key, lease, messages, limits, consumedAt);
}
