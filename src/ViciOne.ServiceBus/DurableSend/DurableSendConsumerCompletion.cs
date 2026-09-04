using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Diagnostics;

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Process-local, generation-fenced completion capability handed to a volatile transport adapter for one dispatch.</summary>
internal sealed class DurableSendConsumerCompletion<TBus> : IDurableSendConsumerCompletion
    where TBus : class, IBus
{
    readonly Guid _generationToken;
    readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    readonly long _startedTimestamp;
    readonly IDurableSendStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public DurableSendConsumerCompletion(
        DurableSendId durableSendId,
        Guid generationToken,
        IDurableSendStore<TBus> store,
        TimeProvider timeProvider,
        V5ServiceBusInstrumentation<TBus> instrumentation)
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
