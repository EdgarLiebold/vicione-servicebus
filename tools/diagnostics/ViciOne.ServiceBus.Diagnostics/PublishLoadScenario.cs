namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMqTransport;


/// <summary>
/// Publishes a large number of messages concurrently and waits until every one of them is consumed.
/// <para>
/// Every message carries an identity this run owns. The wait ends when every one of them has arrived
/// at least once; the verdict is taken afterwards, and it is a different question: exact means every
/// identity seen exactly once, nothing twice, and nothing seen that this run never published.
/// </para>
/// <para>
/// Between the two there is a drain window, and the result names it. Without one, a duplicate that the
/// broker delivers a moment after the last first-seen identity would be reported as an exact run. The
/// window is not a proof that nothing will ever arrive again - the consumer is attached until the bus
/// stops - and the result says how long it was rather than implying forever.
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

    public static async Task<object> Run(int messages, int concurrencyLimit, int prefetchCount,
        TimeSpan completionLimit, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHost("test", cancellationToken);

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
        try
        {
            var elapsed = Stopwatch.StartNew();

            var publishers = new Task[messages];
            for (var index = 0; index < messages; index++)
                publishers[index] = bus.Publish(new LoadPing { Sequence = index }, cancellationToken);

            var handedOver = elapsed.Elapsed;

            await Task.WhenAll(publishers);

            var published = elapsed.Elapsed;

            var allSeen = await ledger.WaitForAllExpected(completionLimit, cancellationToken);
            var completedAt = elapsed.Elapsed;

            // The drain window runs whether or not everything arrived: a run that is missing one
            // identity may still be holding a duplicate of another, and the report has to name both.
            await Task.Delay(DrainWindow, cancellationToken);

            MessageSequenceLedger.Snapshot snapshot = ledger.Read();

            var outcome = !allSeen ? "timeout" : snapshot.IsExact ? "exact" : "invalid";

            return new
            {
                scenario = "publish-load",
                messages,
                concurrencyLimit,
                prefetchCount,
                queue,
                outcome,
                observationBoundary =
                    $"every identity arrived at least once, then {DrainWindow.TotalSeconds:0} s of further observation "
                    + "with the consumer still attached; nothing beyond that window is claimed",
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
            await bus.StopAsync(CancellationToken.None);
        }
    }


    public record LoadPing
    {
        public int Sequence { get; init; }
    }
}
