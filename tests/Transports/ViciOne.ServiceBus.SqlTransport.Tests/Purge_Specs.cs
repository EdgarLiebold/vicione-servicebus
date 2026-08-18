namespace ViciOne.ServiceBus.SqlTransport.Tests;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


/// <summary>
/// Purge on startup removes what is already in the queue. Publishing after the start and consuming it
/// says nothing about that: a queue that was never purged delivers the new message just as well. The
/// message that has to disappear is therefore put into the queue before the endpoint that purges it
/// starts, and what the case asserts is that the consumer never saw it - a delivery count of zero alone
/// would also be satisfied by a consumer that simply ate it.
/// </summary>
[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Purging_a_queue_on_startup<T>
    where T : IDatabaseTestConfiguration, new()
{
    const string Purged = "this message was in the queue before the start";
    const string Delivered = "this message was sent after the purge";

    /// <summary>A fresh queue per run: the transport database outlives a run and would carry its content.</summary>
    readonly string _queue = $"purge-on-startup-queue-{NewId.Next().ToString("N")}";

    [Test]
    public async Task Should_remove_what_was_in_the_queue_before_the_start()
    {
        var dialect = TransportInspection.DialectOf(_configuration);

        RecordingConsumer.Reset();

        // The queue has to exist before anything can be left in it.
        await using (var creator = BuildConsumer(purgeOnStartup: false))
        {
            var creatorHarness = await creator.StartTestHarness();
            await creatorHarness.Stop();
        }

        // A bus with no receive endpoint: it sends into the queue and nothing takes the message out again.
        await using (var sender = BuildSender())
        {
            var senderHarness = await sender.StartTestHarness();

            var endpoint = await senderHarness.Bus.GetSendEndpoint(new Uri($"queue:{_queue}"));
            await endpoint.Send(new PurgeMessage(Purged));

            await senderHarness.Stop();

            await using var connection = await sender.OpenTransport(dialect);

            Assert.That(await connection.DeliveryCount(dialect, TransportSchema.Name, _queue, 1), Is.EqualTo(1),
                "the fixture could not leave a message in the queue, so there is nothing for the purge to remove");
        }

        await using var provider = BuildConsumer(purgeOnStartup: true);

        var harness = await provider.StartTestHarness();

        // A message the endpoint does consume, so the assertion below runs against a live endpoint and not
        // against one that happens to be idle.
        var live = await harness.Bus.GetSendEndpoint(new Uri($"queue:{_queue}"));
        await live.Send(new PurgeMessage(Delivered));

        Assert.That(await harness.Consumed.Any<PurgeMessage>(), Is.True, "the purge left an endpoint that no longer consumes");

        await harness.InactivityTask;

        await using var after = await provider.OpenTransport(dialect);

        Assert.Multiple(() =>
        {
            Assert.That(RecordingConsumer.Values, Does.Not.Contain(Purged),
                "the message that was in the queue before the start reached the consumer, so it was delivered instead of purged");
            Assert.That(RecordingConsumer.Values, Does.Contain(Delivered),
                "the message sent after the purge never arrived");
        });

        Assert.That(await after.DeliveryCount(dialect, TransportSchema.Name, _queue, 1), Is.Zero,
            "the queue still holds a delivery after everything was accounted for");

        await harness.Stop();
    }

    ServiceProvider BuildConsumer(bool purgeOnStartup)
    {
        return _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(3), testTimeout: TimeSpan.FromSeconds(60));
                x.AddConsumer<RecordingConsumer>();

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(_queue, e =>
                    {
                        e.PurgeOnStartup = purgeOnStartup;
                        e.ConfigureConsumeTopology = false;

                        e.ConfigureConsumer<RecordingConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);
    }

    ServiceProvider BuildSender()
    {
        return _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                _configuration.Configure(x, (_, _) =>
                {
                });
            })
            .BuildServiceProvider(true);
    }

    readonly T _configuration;

    public Purging_a_queue_on_startup()
    {
        _configuration = new T();
    }


    public record PurgeMessage(string Value);


    /// <summary>Records what it was given, so the case can say which message reached it.</summary>
    public class RecordingConsumer :
        IConsumer<PurgeMessage>
    {
        static ConcurrentQueue<string> _values = new();

        public static string[] Values => _values.ToArray();

        public static void Reset()
        {
            _values = new ConcurrentQueue<string>();
        }

        public Task Consume(ConsumeContext<PurgeMessage> context)
        {
            _values.Enqueue(context.Message.Value);

            return Task.CompletedTask;
        }
    }
}
