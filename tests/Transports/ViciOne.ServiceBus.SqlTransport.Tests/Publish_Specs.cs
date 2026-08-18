namespace ViciOne.ServiceBus.SqlTransport.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;
using UnitTests;


[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Using_publish<T>
    where T : IDatabaseTestConfiguration, new()
{
    [Test]
    public async Task Should_consume_a_lot_of_published_messages()
    {
        // A fresh queue per run: the transport database outlives a run, and a queue that keeps its name
        // still holds what an earlier fixture published into it, so no exact set could be asserted.
        var queue = $"publish-input-queue-{NewId.Next().ToString("N")}";

        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(TextWriter.Null, x =>
            {
                x.AddConsumer<TestMessageConsumer>();
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(2));

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(queue, e =>
                    {
                        e.PrefetchCount = 30;

                        e.ConfigureConsumer<TestMessageConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        const int limit = 1000;

        var published = Enumerable.Range(0, limit).Select(i => $"Hello, World! {i}").ToArray();

        await Parallel.ForEachAsync(published, options, async (value, token) =>
        {
            await harness.Bus.Publish(new TestMessage(value), token);
        });

        var consumed = await harness.Consumed.SelectAsync<TestMessage>().Take(limit)
            .Select(x => x.Context.Message.Value).ToListAsync();

        Assert.That(consumed, Is.EquivalentTo(published),
            "the messages that arrived are not exactly the ones that were published");

        await harness.Stop();
    }

    readonly T _configuration;

    public Using_publish()
    {
        _configuration = new T();
    }
}


[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Publishing_a_unsubscribed_message_type<T>
    where T : IDatabaseTestConfiguration, new()
{
    /// <summary>
    /// Publishing a message type nobody subscribes to leaves no delivery and no message row.
    /// <para>
    /// Both dialects promise exactly this in their publish procedure: it counts the deliveries it
    /// created and, when that count is zero, deletes the message row it had just written. The case
    /// therefore asks about that one row and its deliveries, chosen by an identifier the caller sets,
    /// rather than about a count over a table that every fixture ever run against this database shares.
    /// </para>
    /// <para>
    /// The subscribed publish is the control, and it is not decoration. Without it, an absent row would
    /// also be what a publish that never reached the transport leaves behind, and the case would pass
    /// for the wrong reason. It proves in the same run, against the same endpoint, that a publish does
    /// arrive and does leave a delivery in this queue.
    /// </para>
    /// <para>
    /// The unsubscribed contract belongs to this fixture and to nothing else. That is not tidiness: the
    /// transport database outlives a fixture, and so do its queues and their subscriptions. Publishing
    /// the shared TestMessage contract here landed a delivery in a queue named testmessage that another
    /// fixture had left behind in an earlier run, so the type was subscribed and the case was measuring
    /// the opposite of its own name.
    /// </para>
    /// </summary>
    [Test]
    public async Task Should_leave_neither_a_delivery_nor_a_message_row()
    {
        var dialect = TransportInspection.DialectOf(_configuration);
        var queue = $"orphan-input-queue-{NewId.Next().ToString("N")}";

        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddConsumer<SubscribedMessageConsumer>();
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(3));

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(queue, e =>
                    {
                        e.PrefetchCount = 30;

                        e.ConfigureConsumer<SubscribedMessageConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        var subscribed = NewId.NextGuid();
        var unsubscribed = NewId.NextGuid();

        await harness.Bus.Publish(new SubscribedMessage("somebody subscribes to this"),
            context => context.MessageId = subscribed, harness.CancellationToken);

        Assert.That(await harness.Consumed.Any<SubscribedMessage>(), Is.True,
            "the control message was never consumed, so this run cannot say what a publish does");

        await harness.Bus.Publish(new UnsubscribedMessage("nobody subscribes to this"),
            context => context.MessageId = unsubscribed, harness.CancellationToken);

        // Inactivity, not a fixed wait: the endpoint reports that nothing is arriving any more.
        await harness.InactivityTask;

        await using (var connection = await provider.OpenTransport(dialect))
        {
            var queued = await connection.DeliveryCount(dialect, TransportSchema.Name, queue, 1);
            var deadLettered = await connection.DeliveryCount(dialect, TransportSchema.Name, queue, 3);
            var errored = await connection.DeliveryCount(dialect, TransportSchema.Name, queue, 2);

            var rowExists = await connection.MessageExists(dialect, TransportSchema.Name, unsubscribed);
            var deliveries = await connection.DeliveryCountForMessage(dialect, TransportSchema.Name, unsubscribed);
            IReadOnlyList<string> holders = await connection.QueuesHoldingMessage(dialect, TransportSchema.Name, unsubscribed);

            Assert.Multiple(() =>
            {
                Assert.That(rowExists, Is.False,
                    "the publish created no delivery, so its message row had to be removed and it is still there");
                Assert.That(deliveries, Is.Zero,
                    $"the unsubscribed message has a delivery in: {string.Join(", ", holders)}");
                Assert.That(queued, Is.Zero, "the unsubscribed message was delivered into the queue anyway");
                Assert.That(errored, Is.Zero, "the unsubscribed message ended up in the error queue");
                Assert.That(deadLettered, Is.Zero, "the unsubscribed message ended up in the dead letter queue");
            });
        }

        await harness.Stop();
    }

    readonly T _configuration;

    public Publishing_a_unsubscribed_message_type()
    {
        _configuration = new T();
    }


    /// <summary>The control contract. Something consumes it, so publishing it must leave a delivery.</summary>
    public record SubscribedMessage(string Value);


    /// <summary>
    /// The subject. Nothing in this repository consumes it, and no other fixture publishes it, so no
    /// queue left behind in the shared transport database can carry a subscription for it.
    /// </summary>
    public record UnsubscribedMessage(string Value);


    class SubscribedMessageConsumer :
        IConsumer<SubscribedMessage>
    {
        public Task Consume(ConsumeContext<SubscribedMessage> context)
        {
            return Task.CompletedTask;
        }
    }
}
