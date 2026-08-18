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
    public async Task Should_not_leave_orphaned_messages()
    {
        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(2));

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint("publish-input-queue", e =>
                    {
                        e.PrefetchCount = 30;
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        await harness.Bus.Publish(new TestMessage($"Hello, World!"), harness.CancellationToken);

        await harness.Stop();
    }

    readonly T _configuration;

    public Publishing_a_unsubscribed_message_type()
    {
        _configuration = new T();
    }
}
