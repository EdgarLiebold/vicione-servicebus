using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus> : BackgroundService
    where TBus : class, IBus
{
    readonly object _compositionLock = new();
    readonly IReadOnlyList<IDurableSendDispatcher<TBus>>? _dispatchers;
    readonly IReadOnlyList<ITransportSendFailureClassifier> _failureClassifiers;
    IReadOnlyList<IReliableDeliverySource<TBus>> _additionalSources = Array.Empty<IReliableDeliverySource<TBus>>();
    readonly ServiceBusInstrumentation<TBus> _instrumentation;
    readonly ILogger<ReliableMessagingDeliveryService<TBus>> _logger;
    readonly ReliableMessagingPolicy<TBus>? _policy;
    readonly IServiceProvider? _provider;
    readonly IReadOnlyList<IOutboxStore<TBus>>? _stores;
    readonly TimeProvider _timeProvider;
    IDurableSendDispatcher<TBus> _dispatcher = null!;
    IOutboxStore<TBus> _store = null!;
    bool _compositionResolved;
    long _nextTelemetrySnapshotUtcTicks;
    int _telemetrySnapshotHasNoSuccessor;

    // Let ValidateOnStart reject options after the Host has materialized its hosted-service list.
    public ReliableMessagingDeliveryService(
        IServiceProvider provider,
        IEnumerable<ITransportSendFailureClassifier> failureClassifiers,
        TimeProvider timeProvider,
        ILogger<ReliableMessagingDeliveryService<TBus>> logger,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ArgumentNullException.ThrowIfNull(failureClassifiers);
        _failureClassifiers = failureClassifiers.ToArray();
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    internal ReliableMessagingDeliveryService(
        IEnumerable<IOutboxStore<TBus>> stores,
        IEnumerable<IDurableSendDispatcher<TBus>> dispatchers,
        IEnumerable<ITransportSendFailureClassifier> failureClassifiers,
        ReliableMessagingPolicy<TBus> policy,
        TimeProvider timeProvider,
        ILogger<ReliableMessagingDeliveryService<TBus>> logger,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        ArgumentNullException.ThrowIfNull(stores);
        ArgumentNullException.ThrowIfNull(dispatchers);
        ArgumentNullException.ThrowIfNull(failureClassifiers);
        _stores = stores.ToArray();
        _dispatchers = dispatchers.ToArray();
        _failureClassifiers = failureClassifiers.ToArray();
        _additionalSources = Array.Empty<IReliableDeliverySource<TBus>>();
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EnsureCompositionResolved();
        while (!stoppingToken.IsCancellationRequested)
        {
            bool didWork = await DeliverDueBatchAsync(stoppingToken).ConfigureAwait(false);
            if (!didWork)
                await WaitForWorkAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    internal async Task<bool> DeliverDueBatchAsync(CancellationToken cancellationToken)
    {
        EnsureCompositionResolved();
        IReadOnlyList<DurableSendDelivery> batch = [];
        DateTimeOffset? claimTime = null;
        if (_store != null)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            claimTime = now;
            ReliableMessagingPolicy<TBus> policy = RequirePolicy();
            // Claim no more work than can begin immediately. A durable lease is ownership, not a local work buffer:
            // pre-claiming a larger batch would leave later records leased-but-idle behind a slow provider send.
            IReadOnlyList<DurableSendDelivery> claimed = await _store
                .ClaimDueAsync(now, policy.MaximumConcurrentDeliveries, policy.LeaseDuration, cancellationToken)
                .ConfigureAwait(false);
            batch = ReliableMessagingProviderGuard.ValidateClaims(
                claimed,
                now,
                policy.MaximumConcurrentDeliveries);
        }

        if (batch.Count == 1)
        {
            await DeliverAsync(batch[0], cancellationToken).ConfigureAwait(false);
        }
        else if (batch.Count > 1)
        {
            var deliveries = new Task[batch.Count];
            for (int index = 0; index < batch.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                deliveries[index] = DeliverAsync(batch[index], cancellationToken);
            }

            await Task.WhenAll(deliveries).ConfigureAwait(false);
        }

        bool additionalWork = false;
        foreach (IReliableDeliverySource<TBus> source in _additionalSources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            additionalWork |= await source.DeliverDueBatchAsync(cancellationToken).ConfigureAwait(false);
        }

        if (claimTime is { } anchor)
            await RefreshTelemetrySnapshotIfDueAsync(anchor, cancellationToken).ConfigureAwait(false);
        return batch.Count > 0 || additionalWork;
    }

    async Task WaitForWorkAsync(CancellationToken stoppingToken)
    {
        if (_additionalSources.Count == 0)
        {
            await Task.Delay(RequirePolicy().PollInterval, _timeProvider, stoppingToken).ConfigureAwait(false);
            return;
        }

        using var wakeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var waits = new List<Task>(_additionalSources.Count + (_store == null ? 0 : 1));
        foreach (IReliableDeliverySource<TBus> source in _additionalSources)
            waits.Add(source.WaitForWorkAsync(wakeTokenSource.Token));

        if (_store != null)
            waits.Add(Task.Delay(RequirePolicy().PollInterval, _timeProvider, wakeTokenSource.Token));

        _ = await Task.WhenAny(waits).ConfigureAwait(false);
        await wakeTokenSource.CancelAsync().ConfigureAwait(false);
        try
        {
            await Task.WhenAll(waits).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
        }
    }

    void EnsureCompositionResolved()
    {
        if (Volatile.Read(ref _compositionResolved))
            return;

        lock (_compositionLock)
        {
            if (_compositionResolved)
                return;

            // Provider sources can resolve validated options and a bus while being materialized.
            // Resolve them only after ValidateOnStart, when the Host owns all hosted services.
            if (_provider is not null)
                _additionalSources = _provider.GetServices<IReliableDeliverySource<TBus>>().ToArray();

            IOutboxStore<TBus>[] stores = (_stores ?? _provider!.GetServices<IOutboxStore<TBus>>()).ToArray();
            IDurableSendDispatcher<TBus>[] dispatchers = (_dispatchers ?? _provider!.GetServices<IDurableSendDispatcher<TBus>>()).ToArray();
            if (stores.Length == 0 && _additionalSources.Count > 0)
            {
                Volatile.Write(ref _compositionResolved, true);
                return;
            }

            _ = RequirePolicy();
            _store = ReliableMessagingComposition.RequireExactlyOne<IOutboxStore<TBus>, TBus>(stores, "persistence store");
            _dispatcher = ReliableMessagingComposition.RequireExactlyOne<IDurableSendDispatcher<TBus>, TBus>(dispatchers, "transport dispatcher");
            Volatile.Write(ref _compositionResolved, true);
        }
    }

    ReliableMessagingPolicy<TBus> RequirePolicy() =>
        _policy ?? _provider?.GetService<ReliableMessagingPolicy<TBus>>() ?? throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Reliable messaging",
                "unknown",
                $"A persistence store or transport dispatcher was registered for bus '{typeof(TBus)}' without a delivery policy.",
                "Configure the component inside UseReliableMessaging"));
}
