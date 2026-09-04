using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMqTransport;

#nullable enable
namespace ViciOne.ServiceBus.Diagnostics;
/// <summary>
/// Publishes a large number of messages concurrently and waits until every one of them is consumed.
/// <para>
/// Every message carries an identity this run owns. The wait ends when every one of them has arrived
/// at least once; the verdict is taken afterwards, and it is a different question: exact means every
/// identity seen exactly once, nothing twice, and nothing seen that this run never published.
/// </para>
/// <para>
/// Three steps stand between the wait and the verdict, and the result names all three. An observation
/// window, because a duplicate the broker delivers a moment after the last first-seen identity would
/// otherwise be reported as an exact run. Then a bounded stop of the bus. Then the snapshot. Reading
/// it while a handler could still run made the verdict a race - a scan that has already passed an
/// identity does not see the duplicate that arrives behind it, and the reported total belongs to no
/// single moment of the run.
/// <para>
/// What that stop proves is stated exactly, because the transport does not support a stronger
/// sentence: a stop that <em>finished while its budget still held</em> is interpreted as quiescence,
/// since the consumer agent awaits its delivery-complete signal in that case. A stop whose budget
/// expired proves nothing - the same method catches that cancellation, cancels the pending consumers
/// and completes anyway - and is reported as inconclusive, which can never be exact.
/// </para>
/// </para>
/// <para>
/// Publisher confirmation is off on purpose: the subject is the endpoint under a burst, and
/// confirmation would measure the broker's acknowledgement path instead.
/// </para>
/// </summary>
static class PublishLoadScenario
{
    /// <summary>How long the scenario keeps observing after the last expected identity arrived.</summary>
    public static readonly TimeSpan DrainWindow = TimeSpan.FromSeconds(3);

    /// <summary>How long the bounded stop is given to bring the consumer to a standstill.</summary>
    public static readonly TimeSpan QuiescenceBudget = TimeSpan.FromSeconds(30);

    public static async Task<object> RunAsync(int messages, int concurrencyLimit, int prefetchCount,
        TimeSpan completionLimit, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHostAsync("test", cancellationToken);

        (var host, var port, var username, var password) = RunScopedBroker.Read();

        var queue = $"diagnostics-publish-load-{NewId.Next().ToString("N")}";
        var ledger = new MessageSequenceLedger(messages);

        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(host, (ushort)port, "test", h =>
            {
                h.Username(username);
                h.Password(password);
                h.PublisherConfirmation = false;
            });

            cfg.ReceiveEndpoint(queue, e =>
            {
                e.AutoDelete = true;
                e.Durable = false;
                e.PrefetchCount = prefetchCount;

                e.UseConcurrencyLimit(concurrencyLimit);

                e.Handler<LoadPing>(context =>
                {
                    ledger.Observed(context.Message.Sequence);

                    return Task.CompletedTask;
                });
            });
        });

        await bus.StartAsync(cancellationToken);
        var alreadyStopped = false;
        try
        {
            var elapsed = Stopwatch.StartNew();

            var publishers = new Task[messages];
            for (var index = 0; index < messages; index++)
                publishers[index] = bus.PublishAsync(new LoadPing { Sequence = index }, cancellationToken);

            var handedOver = elapsed.Elapsed;

            await Task.WhenAll(publishers);

            var published = elapsed.Elapsed;

            var allSeen = await ledger.WaitForAllExpectedAsync(completionLimit, cancellationToken);
            var completedAt = elapsed.Elapsed;

            (var quiesced, MessageSequenceLedger.Snapshot snapshot) = await ObserveThenQuiesceThenReadAsync(
                ledger, token => bus.StopAsync(token), DrainWindow, QuiescenceBudget, cancellationToken);
            alreadyStopped = true;

            var outcome = Outcome(allSeen, quiesced, snapshot.IsExact);

            return new
            {
                scenario = "publish-load",
                messages,
                concurrencyLimit,
                prefetchCount,
                queue,
                outcome,
                observationBoundary =
                    $"every identity arrived at least once, then {DrainWindow.TotalSeconds:0} s of further "
                    + "observation with the consumer attached, then a bounded stop of the bus, then the "
                    + "snapshot. A stop that finished inside its budget is interpreted as quiescence; a stop "
                    + "whose budget expired is reported as inconclusive and can never be exact. Nothing that "
                    + "a later run of the same queue might see is claimed",
                quiesced,
                handedOverMilliseconds = (long)handedOver.TotalMilliseconds,
                publishedMilliseconds = (long)published.TotalMilliseconds,
                completedMilliseconds = allSeen ? (long?)completedAt.TotalMilliseconds : null,
                publishedPerSecond = (long)(messages / published.TotalSeconds),
                // Only for an exact set. A rate for a run that timed out or observed a duplicate is a
                // number that invites the wrong conclusion.
                completedPerSecond = outcome == "exact" ? (long?)(messages / completedAt.TotalSeconds) : null,
                uniqueConsumed = snapshot.UniqueConsumed,
                observationCount = snapshot.ObservationCount,
                missing = snapshot.Missing,
                duplicates = snapshot.Duplicates,
                outOfRange = snapshot.OutOfRange
            };
        }
        finally
        {
            // Only when the quiescing stop above did not already run. It is the same stop; issuing it
            // twice would ask an already stopped bus to stop again.
            if (!alreadyStopped)
                await bus.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// The order the verdict depends on: observe, come to a standstill, then read.
    /// <para>
    /// The observation window runs whether or not everything arrived - a run that is missing one
    /// identity may still be holding a duplicate of another, and the report has to name both. The stop
    /// follows, because a scan that runs against live handlers belongs to no single moment of the run.
    /// The read is last, and it is the only place the exactness of the set is decided.
    /// </para>
    /// <para>
    /// It is one method so the order is one thing that can be shown to hold, rather than three
    /// statements in a row that happen to be written down in that sequence today.
    /// </para>
    /// </summary>
    /// <param name="ledger">The ledger used by the operation.</param>
    /// <param name="stop">The stop used by the operation.</param>
    /// <param name="window">The window used by the operation.</param>
    /// <param name="budget">The budget used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    internal static async Task<(bool Quiesced, MessageSequenceLedger.Snapshot Snapshot)> ObserveThenQuiesceThenReadAsync(
        MessageSequenceLedger ledger, Func<CancellationToken, Task> stop, TimeSpan window, TimeSpan budget,
        CancellationToken cancellationToken, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        await Task.Delay(window, timeProvider, cancellationToken).ConfigureAwait(false);

        var quiesced = await QuiesceAsync(stop, budget, timeProvider).ConfigureAwait(false);

        return (quiesced, ledger.Read());
    }

    /// <summary>
    /// Stops the consumer within a bound, and says whether that stop may be read as a standstill.
    /// <para>
    /// Measured against this transport rather than assumed. A stop reaches
    /// <c>ConsumerAgent.ActiveAndActualAgentsCompleted</c>, which awaits the delivery-complete signal
    /// the dispatcher raises when its active dispatch count reaches zero. A stop that finished while
    /// its budget still held therefore went through that wait, and this scenario interprets it as
    /// quiescence for the snapshot that follows. That is the exact claim; it is not a general
    /// statement that a bounded stop drains every handler.
    /// </para>
    /// <para>
    /// What it does not prove is the cancelled case, and that is why the budget is read rather than the
    /// exception: on cancellation that same method logs, cancels the pending consumers and completes
    /// the stop regardless. A stop whose budget expired therefore returns normally and proves nothing,
    /// so it is reported as not quiesced instead of being counted as one.
    /// </para>
    /// </summary>
    internal static async Task<bool> QuiesceAsync(Func<CancellationToken, Task> stop, TimeSpan budget,
        TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        using var bounded = new CancellationTokenSource(budget, timeProvider);

        try
        {
            await stop(bounded.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return !bounded.IsCancellationRequested;
    }

    /// <summary>
    /// The verdict of a run, from the three questions that decide it.
    /// <para>
    /// Exactness is the strongest claim and it needs both of the others: everything arrived, and the
    /// snapshot was taken when nothing could still be counting. A snapshot read against live handlers
    /// is not evidence of exactness even when it looks exact, so that case is named rather than
    /// reported as a success.
    /// </para>
    /// </summary>
    internal static string Outcome(bool allExpectedSeen, bool quiesced, bool exact)
    {
        if (!allExpectedSeen)
            return "timeout";

        if (!quiesced)
            return "inconclusive";

        return exact ? "exact" : "invalid";
    }


    public record LoadPing
    {
        public int Sequence { get; init; }
    }
}
