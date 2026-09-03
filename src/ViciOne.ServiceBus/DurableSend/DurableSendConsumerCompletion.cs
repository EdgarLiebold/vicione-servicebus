namespace ViciOne.ServiceBus.DurableSend;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Diagnostics;

/// <summary>Process-local, generation-fenced completion capability handed to a volatile transport adapter for one dispatch.</summary>
internal sealed class DurableSendConsumerCompletion<TBus> : IDurableSendConsumerCompletion
    where TBus : class, IBus
{
    readonly Guid _generationToken;
    readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    readonly long _startedTimestamp = Stopwatch.GetTimestamp();
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
    }

    public DurableSendId DurableSendId { get; }

    public async ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default)
    {
        bool retired = await _store
            .CompleteConsumerDeliveryAsync(DurableSendId, _generationToken, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        _instrumentation.RecordDurableConsumerCompletion(
            retired,
            Stopwatch.GetElapsedTime(_startedTimestamp).TotalSeconds);
        return retired;
    }
}
