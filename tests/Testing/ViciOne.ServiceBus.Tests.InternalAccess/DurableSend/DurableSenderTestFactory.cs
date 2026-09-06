namespace ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;

/// <summary>Signed, xUnit-free access bridge for the internal durable-sender state machines.</summary>
public static class DurableSenderTestFactory
{
    public static IOutboxStore<TBus> CreateInMemoryStore<TBus>()
        where TBus : class, IBus
        => new InMemoryReliableStore<TBus>();

    public static DurableSenderDeliveryTestDriver<TBus> CreateDeliveryDriver<TBus>(
        IOutboxStore<TBus> store,
        IDurableSendDispatcher<TBus> dispatcher,
        TimeProvider timeProvider,
        Action<ReliableMessagingOptions<TBus>>? configure = null,
        IEnumerable<ITransportSendFailureClassifier>? classifiers = null)
        where TBus : class, IBus
        => new(store, dispatcher, timeProvider, configure, classifiers);
}
