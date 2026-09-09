using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Process-local, generation-fenced completion capability handed to a volatile transport adapter for one dispatch.</summary>
/// <typeparam name="TBus">The bus that owns the persisted intent.</typeparam>
internal sealed class DurableSendConsumerCompletion<TBus> : IDurableSendConsumerCompletion
    where TBus : class, IBus
{
    readonly Guid _generationToken;
    readonly ServiceBusInstrumentation<TBus> _instrumentation;
    readonly long _startedTimestamp;
    readonly IOutboxStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public DurableSendConsumerCompletion(
        DurableSendId durableSendId,
        Guid generationToken,
        IOutboxStore<TBus> store,
        TimeProvider timeProvider,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        DurableSendId = durableSendId;
        if (generationToken == Guid.Empty)
            throw new ArgumentException("Generation token must not be empty.", nameof(generationToken));
        _generationToken = generationToken;
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        _startedTimestamp = _timeProvider.GetTimestamp();
    }

    public DurableSendId DurableSendId { get; }

    public async ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default)
    {
        bool retired = await _store
            .CompleteConsumerDeliveryAsync(DurableSendId, _generationToken, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        _instrumentation.RecordDurableConsumerCompletion(
            retired,
            _timeProvider.GetElapsedTime(_startedTimestamp).TotalSeconds);
        return retired;
    }
}
