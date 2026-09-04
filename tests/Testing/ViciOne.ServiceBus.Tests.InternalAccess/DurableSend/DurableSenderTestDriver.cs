using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.DurableSend;

namespace ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
/// <summary>Signed, xUnit-free access bridge for the internal durable-sender state machines.</summary>
public static class DurableSenderTestFactory
{
    public static IDurableSendStore<TBus> CreateInMemoryStore<TBus>()
        where TBus : class, IBus
        => new InMemoryDurableSendStore<TBus>();

    public static DurableSenderDeliveryTestDriver<TBus> CreateDeliveryDriver<TBus>(
        IDurableSendStore<TBus> store,
        IDurableSendDispatcher<TBus> dispatcher,
        TimeProvider timeProvider,
        Action<DurableSenderOptions<TBus>>? configure = null,
        IEnumerable<ITransportSendFailureClassifier>? classifiers = null)
        where TBus : class, IBus
        => new(store, dispatcher, timeProvider, configure, classifiers);
}

public sealed class DurableSenderDeliveryTestDriver<TBus> : IDisposable
    where TBus : class, IBus
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    private readonly DurableSenderDeliveryService<TBus> _service;

    internal DurableSenderDeliveryTestDriver(
        IDurableSendStore<TBus> store,
        IDurableSendDispatcher<TBus> dispatcher,
        TimeProvider timeProvider,
        Action<DurableSenderOptions<TBus>>? configure,
        IEnumerable<ITransportSendFailureClassifier>? classifiers)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var options = new DurableSenderOptions<TBus>();
        configure?.Invoke(options);
        DurableSenderPolicy<TBus> policy = options.ValidateAndFreeze();
        _instrumentation = new V5ServiceBusInstrumentation<TBus>(_meterFactory);
        _service = new DurableSenderDeliveryService<TBus>(
            [store],
            [dispatcher],
            classifiers ?? [],
            policy,
            timeProvider,
            NullLogger<DurableSenderDeliveryService<TBus>>.Instance,
            _instrumentation);
    }

    public Task<bool> DeliverDueBatch(CancellationToken cancellationToken = default)
        => _service.DeliverDueBatchAsync(cancellationToken);

    public TimeSpan CalculateRetryDelay(DurableSendId id, int attempt)
        => _service.CalculateRetryDelay(id, attempt);

    public IDurableSendConsumerCompletion CreateCompletion(
        DurableSendId id,
        Guid generationToken,
        IDurableSendStore<TBus> store,
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
