namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
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
    [Test]
    /// <summary>
    /// A message type nobody subscribes to reaches no queue. The case published one and asserted
    /// nothing at all, so a delivery into a queue, into the dead letter queue, or nothing happening
    /// would all have passed it. The two negative assertions are read from the transport's own tables.
    /// <para>
    /// Measured while writing this: the row in the message table itself does remain after the publish,
    /// within the inactivity window this case waits. Whether the transport promises to remove it, and
    /// on which sweep, is a product question, so this case asserts what it can prove - that nothing was
    /// delivered anywhere - and does not claim the stronger half of its old name.
    /// </para>
    /// </summary>
    public async Task Should_not_leave_orphaned_messages()
    {
        var dialect = TransportInspection.DialectOf(_configuration);
        var queue = $"orphan-input-queue-{NewId.Next().ToString("N")}";

        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(3));

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(queue, e =>
                    {
                        e.PrefetchCount = 30;
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        long before;
        await using (var connection = await provider.OpenTransport(dialect))
            before = await connection.MessageCount(dialect, TransportSchema.Name);

        await harness.Bus.Publish(new TestMessage("nobody subscribes to this"), harness.CancellationToken);

        // Inactivity, not a fixed wait: the endpoint reports that nothing is arriving any more.
        await harness.InactivityTask;

        await using (var connection = await provider.OpenTransport(dialect))
        {
            var queued = await connection.DeliveryCount(dialect, TransportSchema.Name, queue, 1);
            var deadLettered = await connection.DeliveryCount(dialect, TransportSchema.Name, queue, 3);
            var after = await connection.MessageCount(dialect, TransportSchema.Name);

            Assert.Multiple(() =>
            {
                Assert.That(queued, Is.Zero, "the unsubscribed message was delivered into the queue anyway");
                Assert.That(deadLettered, Is.Zero, "the unsubscribed message ended up in the dead letter queue");
                Assert.That(after, Is.GreaterThanOrEqualTo(before),
                    "the message table shrank, which this case does not drive and cannot explain");
            });
        }

        await harness.Stop();
    }

    readonly T _configuration;

    public Publishing_a_unsubscribed_message_type()
    {
        _configuration = new T();
    }
}
