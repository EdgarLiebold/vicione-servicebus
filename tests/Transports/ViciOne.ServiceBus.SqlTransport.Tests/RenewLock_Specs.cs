namespace ViciOne.ServiceBus.SqlTransport.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


/// <summary>
/// A consumer that runs longer than the lock it holds keeps the message only because the transport
/// renews that lock. The imported fixture ran a two minute consumer under the one minute default and
/// asserted that a message had been consumed at all, which a redelivered duplicate satisfies just as
/// well - and two minutes of wall clock proved nothing that a shortened lock does not prove in seconds.
/// <para>
/// The lock is a second long here and the consumer holds the message across several of them. The case
/// asserts that the consumer was entered exactly once, that it finished, and that no second delivery of
/// the same message arrived while it was running.
/// </para>
/// </summary>
[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Using_a_slow_consumer<T>
    where T : IDatabaseTestConfiguration, new()
{
    /// <summary>
    /// Short enough to expire several times while the consumer works, long enough to be renewable under a
    /// loaded run. The transport renews at seventy percent of the duration, so a one second lock leaves
    /// seven hundred milliseconds of headroom, and a full suite took longer than that: the case then
    /// reported a second entry, which is the finding it is built to report and not a flake to retry away.
    /// </summary>
    static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(4);

    const string Queue = "slow-consumer-queue";

    [Test]
    public async Task Should_renew_the_lock_and_deliver_the_message_once()
    {
        SlowConsumer.Reset();

        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(20), testTimeout: TimeSpan.FromSeconds(120));
                x.AddConsumer<SlowConsumer>();

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(Queue, e =>
                    {
                        e.LockDuration = LockDuration;
                        e.MaxLockDuration = TimeSpan.FromMinutes(5);
                        e.PollingInterval = TimeSpan.FromMilliseconds(200);

                        e.ConfigureConsumer<SlowConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();

        var endpoint = await harness.Bus.GetSendEndpoint(new Uri($"queue:{Queue}"));
        await endpoint.Send(new SlowMessage(NewId.NextGuid()));

        // The consumer reports that it is inside; the lock has to be renewed from here on.
        await SlowConsumer.Started.WaitAsync(harness.CancellationToken);

        var dialect = TransportInspection.DialectOf(_configuration);

        DateTime? firstExpiry;
        DateTime? renewedExpiry;

        await using (var held = await provider.OpenTransport(dialect))
        {
            firstExpiry = await held.LockExpiry(dialect, TransportSchema.Name, Queue);

            // The consumer is held across several lock durations. This is the one place where time has to
            // pass, because the lock expiring is the situation under test; what ends the wait is the
            // release below and not a timeout.
            for (var expiry = 0; expiry < 3; expiry++)
                await Task.Delay(LockDuration, harness.CancellationToken);

            renewedExpiry = await held.LockExpiry(dialect, TransportSchema.Name, Queue);
        }

        SlowConsumer.Release();

        Assert.That(await harness.Consumed.Any<SlowMessage>(), Is.True, "the message was never consumed");

        await harness.Stop();

        await using var connection = await provider.OpenTransport(dialect);

        Assert.Multiple(() =>
        {
            Assert.That(firstExpiry, Is.Not.Null, "the delivery was not locked while the consumer was inside it");
            Assert.That(renewedExpiry, Is.Not.Null, "the delivery left the queue while the consumer was still inside it");
            Assert.That(renewedExpiry, Is.GreaterThan(firstExpiry),
                "the lock expiry never moved, so nothing renewed the lock while the consumer held the message");
            Assert.That(SlowConsumer.Entered, Is.EqualTo(1),
                "the consumer was entered more than once, so the lock expired and the message was delivered again");
            Assert.That(SlowConsumer.Completed, Is.True, "the consumer never finished");
        });

        Assert.That(await connection.DeliveryCount(dialect, TransportSchema.Name, Queue, 1), Is.Zero,
            "the delivery is still in the queue, so the completed consumer did not release the message");
    }

    readonly T _configuration;

    public Using_a_slow_consumer()
    {
        _configuration = new T();
    }


    public record SlowMessage(Guid Id);


    /// <summary>
    /// Enters once, reports it, and stays inside until the fixture releases it. Counting the entries is
    /// what separates a renewed lock from an expired one: an expired lock produces a second entry.
    /// </summary>
    public class SlowConsumer :
        IConsumer<SlowMessage>
    {
        static TaskCompletionSource<bool> _started = New();
        static TaskCompletionSource<bool> _release = New();
        static int _entered;
        static int _completed;

        public static Task<bool> Started => _started.Task;
        public static int Entered => Volatile.Read(ref _entered);
        public static bool Completed => Volatile.Read(ref _completed) > 0;

        public static void Reset()
        {
            _started = New();
            _release = New();
            Volatile.Write(ref _entered, 0);
            Volatile.Write(ref _completed, 0);
        }

        public static void Release()
        {
            _release.TrySetResult(true);
        }

        public async Task Consume(ConsumeContext<SlowMessage> context)
        {
            Interlocked.Increment(ref _entered);
            _started.TrySetResult(true);

            await _release.Task.WaitAsync(context.CancellationToken).ConfigureAwait(false);

            Interlocked.Increment(ref _completed);
        }

        static TaskCompletionSource<bool> New()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
