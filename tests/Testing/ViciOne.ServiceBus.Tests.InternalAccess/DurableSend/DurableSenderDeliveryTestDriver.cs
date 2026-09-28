using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;

public sealed class DurableSenderDeliveryTestDriver<TBus> : IDisposable
    where TBus : class, IBus
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly ServiceBusInstrumentation<TBus> _instrumentation;
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
        _instrumentation = new ServiceBusInstrumentation<TBus>(_meterFactory);
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

    public object MeterScope => _meterFactory;

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
