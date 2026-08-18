namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMqTransport;


/// <summary>
/// Publishes a large number of messages concurrently and waits until every one of them is consumed.
/// <para>
/// Every message carries a sequence this run owns, and completion means every one of those sequences
/// was seen exactly once. A count of consume events cannot say that: one message delivered twice and
/// another lost reaches the same number, and the scenario would report "all consumed" about a run that
/// lost a message. So the consumer keeps the set, and the result names what it found - how many
/// distinct sequences arrived, which are missing, which arrived more than once, and whether anything
/// arrived that this run never published.
/// </para>
/// <para>
/// Completion is part of the measurement, not a timeout that ends it. A rate for messages handed to the
/// transport says how fast the client can enqueue; the number this scenario exists for is the rate at
/// which they came out the other end, and it cannot be reported before the last one did. Publisher
/// confirmation is off on purpose: the subject is the endpoint under a burst, and confirmation would
/// measure the broker's acknowledgement path instead.
/// </para>
/// </summary>
static class PublishLoadScenario
{
    public static async Task<object> Run(int messages, int concurrencyLimit, int prefetchCount,
        TimeSpan completionLimit, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHost("test", cancellationToken);

        (var host, var port, var username, var password) = RunScopedBroker.Read();

        var queue = $"diagnostics-publish-load-{NewId.Next().ToString("N")}";
        var ledger = new SequenceLedger(messages);

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
            {
                var sequence = index;
                publishers[index] = bus.Publish(new LoadPing { Sequence = sequence }, cancellationToken);
            }

            var handedOver = elapsed.Elapsed;

            await Task.WhenAll(publishers);

            var published = elapsed.Elapsed;

            var complete = await ledger.Complete(completionLimit, cancellationToken);

            var finished = elapsed.Elapsed;

            SequenceLedger.Report report = ledger.Read();

            return new
            {
                scenario = "publish-load",
                messages,
                concurrencyLimit,
                prefetchCount,
                queue,
                complete,
                handedOverMilliseconds = (long)handedOver.TotalMilliseconds,
                publishedMilliseconds = (long)published.TotalMilliseconds,
                completedMilliseconds = (long)finished.TotalMilliseconds,
                publishedPerSecond = (long)(messages / published.TotalSeconds),
                // Only meaningful when every sequence arrived exactly once. Reported as null otherwise,
                // because a rate for an incomplete set is a number that invites the wrong conclusion.
                completedPerSecond = complete ? (long?)(messages / finished.TotalSeconds) : null,
                uniqueConsumed = report.UniqueConsumed,
                missing = report.Missing,
                duplicates = report.Duplicates,
                outOfRange = report.OutOfRange
            };
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }


    /// <summary>
    /// The exact set this run published, and what actually arrived against it.
    /// <para>
    /// It reports rather than judges, but it cannot be satisfied by the wrong messages: completion is
    /// every sequence seen once, so a duplicate does not fill the gap a lost message left.
    /// </para>
    /// </summary>
    sealed class SequenceLedger
    {
        readonly int _expected;
        readonly int[] _seen;
        readonly ConcurrentBag<int> _outOfRange = new();
        readonly TaskCompletionSource<bool> _complete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _distinct;

        public SequenceLedger(int expected)
        {
            _expected = expected;
            _seen = new int[expected];
        }

        public void Observed(int sequence)
        {
            if (sequence < 0 || sequence >= _expected)
            {
                _outOfRange.Add(sequence);
                return;
            }

            if (Interlocked.Increment(ref _seen[sequence]) == 1 && Interlocked.Increment(ref _distinct) == _expected)
                _complete.TrySetResult(true);
        }

        public async Task<bool> Complete(TimeSpan limit, CancellationToken cancellationToken)
        {
            Task delay = Task.Delay(limit, cancellationToken);

            return await Task.WhenAny(_complete.Task, delay) != delay;
        }

        public Report Read()
        {
            var missing = new List<int>();
            var duplicates = new List<int>();

            for (var sequence = 0; sequence < _expected; sequence++)
            {
                var count = Volatile.Read(ref _seen[sequence]);
                if (count == 0)
                    missing.Add(sequence);
                else if (count > 1)
                    duplicates.Add(sequence);
            }

            return new Report(Volatile.Read(ref _distinct), Summarise(missing), Summarise(duplicates),
                Summarise(_outOfRange.ToList()));
        }

        /// <summary>
        /// The count and the first few, not the whole list. A hundred thousand missing sequences would
        /// bury the rest of the result, and the first few are what a reader looks at.
        /// </summary>
        static Detail Summarise(List<int> sequences)
        {
            sequences.Sort();

            return new Detail(sequences.Count, sequences.Take(20).ToArray());
        }


        public readonly record struct Detail(int Count, int[] First);


        public readonly record struct Report(int UniqueConsumed, Detail Missing, Detail Duplicates, Detail OutOfRange);
    }


    public record LoadPing
    {
        public int Sequence { get; init; }
    }
}
