using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Diagnostics;

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

public sealed class DurableSenderDeliveryTestDriver<TBus> : IDisposable
    where TBus : class, IBus
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    private readonly ReliableMessagingDeliveryService<TBus> _service;

    internal DurableSenderDeliveryTestDriver(
        IOutboxStore<TBus> store,
        IDurableSendDispatcher<TBus> dispatcher,
        TimeProvider timeProvider,
        Action<ReliableMessagingOptions<TBus>>? configure,
        IEnumerable<ITransportSendFailureClassifier>? classifiers)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var options = new ReliableMessagingOptions<TBus>
        {
            MaximumStoredCount = 10_000,
            MaximumStoredBytes = 16 * 1024 * 1024,
            Retention = TimeSpan.FromDays(7),
            StoreLimitsConfigured = true,
            DeliveryConfigured = true,
            RetentionConfigured = true,
        };
        configure?.Invoke(options);
        ReliableMessagingPolicy<TBus> policy = options.ValidateAndFreeze();
        _instrumentation = new V5ServiceBusInstrumentation<TBus>(_meterFactory);
        _service = new ReliableMessagingDeliveryService<TBus>(
            [store],
            [dispatcher],
            classifiers ?? [],
            policy,
            timeProvider,
            NullLogger<ReliableMessagingDeliveryService<TBus>>.Instance,
            _instrumentation);
    }

    public Task<bool> DeliverDueBatchAsync(CancellationToken cancellationToken = default)
        => _service.DeliverDueBatchAsync(cancellationToken);

    public TimeSpan CalculateRetryDelay(DurableSendId id, int attempt)
        => _service.CalculateRetryDelay(id, attempt);

    public IDurableSendConsumerCompletion CreateCompletion(
        DurableSendId id,
        Guid generationToken,
        IOutboxStore<TBus> store,
        TimeProvider timeProvider)
        => new DurableSendConsumerCompletion<TBus>(id, generationToken, store, timeProvider, _instrumentation);

    public void Dispose()
    {
        _service.Dispose();
        _instrumentation.Dispose();
        _meterFactory.Dispose();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (Meter meter in _meters)
                meter.Dispose();
        }
    }
}
