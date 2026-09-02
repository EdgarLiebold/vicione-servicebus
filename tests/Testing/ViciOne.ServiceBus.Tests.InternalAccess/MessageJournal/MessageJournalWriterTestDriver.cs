using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;

/// <summary>
/// Test-only access to the journal writer's real failure boundary. It constructs product captures
/// but does not reproduce policy, sizing, timeout, persistence, or telemetry decisions.
/// </summary>
public sealed class MessageJournalWriterTestDriver
{
    private readonly MessageJournalWriter _writer;

    public MessageJournalWriterTestDriver(
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        MessageJournalOptions options)
    {
        _writer = new MessageJournalWriter(store, policy, options);
    }

    public Task ObserveAsync(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        ReadOnlyMemory<byte> body,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        return _writer.ObserveAsync(
            operation,
            outcome,
            cancellationToken,
            () => new MessageJournalCapture(
                operation,
                outcome,
                "application/vnd.vicione.test",
                ["urn:message:ViciOne:JournalTest"],
                metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
                headers ?? new Dictionary<string, string>(StringComparer.Ordinal),
                body));
    }

    public Task ObserveCaptureFailureAsync(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return _writer.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Faulted,
            CancellationToken.None,
            () => throw failure);
    }

    public async Task ObserveWithCanceledTokenAsync(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        ReadOnlyMemory<byte> body)
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        await ObserveAsync(operation, outcome, body, cancellationToken: cancellationSource.Token)
            .ConfigureAwait(false);
    }
}
