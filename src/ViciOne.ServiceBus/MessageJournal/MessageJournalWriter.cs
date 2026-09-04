using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

internal sealed class MessageJournalWriter
{
    private readonly IMessageJournalPolicy _policy;
    private readonly IMessageJournalStore _store;
    private readonly MessageJournalStoreLimits _storeLimits;
    private readonly MessageJournalOptions _options;

    public MessageJournalWriter(
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        MessageJournalOptions options)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _storeLimits = store.Limits
            ?? throw new ArgumentException("The message-journal store must declare finite limits.", nameof(store));
    }

    public async Task ObserveAsync(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        CancellationToken operationCancellationToken,
        Func<MessageJournalCapture> captureFactory)
    {
        ArgumentNullException.ThrowIfNull(captureFactory);

        long startedAt = 0;
        var hasStartTimestamp = false;
        CancellationTokenSource? timeoutSource = null;
        string phase = "clock";

        try
        {
            startedAt = _options.TimeProvider.GetTimestamp();
            hasStartTimestamp = true;

            phase = "timeout_setup";
            using var writeTimeoutSource = new CancellationTokenSource(
                _options.WriteTimeout,
                _options.TimeProvider);
            timeoutSource = writeTimeoutSource;
            using var operationSource = CancellationTokenSource.CreateLinkedTokenSource(
                operationCancellationToken,
                writeTimeoutSource.Token);

            phase = "capture";
            MessageJournalCapture capture = captureFactory();

            phase = "policy";
            MessageJournalProjection? projection = await _policy
                .ProjectAsync(capture, operationSource.Token)
                .ConfigureAwait(false);

            if (projection is null)
            {
                MessageJournalTelemetry.Filtered(operation, outcome, Elapsed(startedAt, hasStartTimestamp));
                return;
            }

            var observedAt = _options.TimeProvider.GetUtcNow();
            var entry = new MessageJournalEntry(
                Guid.CreateVersion7(observedAt),
                observedAt,
                operation,
                outcome,
                projection);

            if (entry.ContentSizeInBytes > _storeLimits.MaximumEntryBytes)
            {
                MessageJournalTelemetry.Failed(
                    operation,
                    outcome,
                    "entry_too_large",
                    Elapsed(startedAt, hasStartTimestamp));
                return;
            }

            phase = "store";
            await _store.AppendAsync(entry, operationSource.Token).ConfigureAwait(false);
            MessageJournalTelemetry.Stored(operation, outcome, Elapsed(startedAt, hasStartTimestamp));
        }
        catch (OperationCanceledException) when (timeoutSource?.IsCancellationRequested == true)
        {
            MessageJournalTelemetry.Failed(
                operation,
                outcome,
                "timeout",
                Elapsed(startedAt, hasStartTimestamp));
        }
        catch (OperationCanceledException)
        {
            MessageJournalTelemetry.Failed(
                operation,
                outcome,
                "cancelled",
                Elapsed(startedAt, hasStartTimestamp));
        }
        catch (Exception)
        {
            MessageJournalTelemetry.Failed(
                operation,
                outcome,
                phase,
                Elapsed(startedAt, hasStartTimestamp));
        }
    }

    private TimeSpan Elapsed(long startedAt, bool hasStartTimestamp)
    {
        if (!hasStartTimestamp)
            return TimeSpan.Zero;

        try
        {
            return _options.TimeProvider.GetElapsedTime(startedAt);
        }
        catch (Exception)
        {
            return TimeSpan.Zero;
        }
    }
}
