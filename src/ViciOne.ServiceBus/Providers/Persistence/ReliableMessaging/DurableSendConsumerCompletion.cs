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
    readonly long? _startedTimestamp;
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
        try
        {
            _startedTimestamp = _timeProvider.GetTimestamp();
        }
        catch
        {
            // Optional duration observation cannot prevent the completion capability from being created.
        }
    }

    public DurableSendId DurableSendId { get; }

    public async ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default)
    {
        bool retired = await _store
            .CompleteConsumerDeliveryAsync(DurableSendId, _generationToken, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        double? elapsedSeconds = null;
        if (_startedTimestamp is { } started)
        {
            try
            {
                elapsedSeconds = _timeProvider.GetElapsedTime(started).TotalSeconds;
            }
            catch
            {
                // The actual store result remains authoritative when duration observation is unavailable.
            }
        }
        _instrumentation.RecordDurableConsumerCompletion(retired, elapsedSeconds);
        return retired;
    }
}
