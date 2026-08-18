namespace ViciOne.ServiceBus.SqlTransport.Tests.SqlServer;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;
using UnitTests;


[TestFixture]
public class Configuring_a_receive_endpoint_without_topology
{
    /// <summary>
    /// A receive endpoint creates its queue and the two queues the transport moves messages into. The
    /// case started and stopped a harness and asserted nothing, so it would have passed with no queue
    /// created at all; it reads the transport's own queue table now.
    /// </summary>
    [Test]
    public async Task Should_create_the_queue()
    {
        await using var provider = new ServiceCollection()
            .ConfigureSqlServerTransport()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<ViciOneServiceBusHostOptions>()
                    .Configure(options => options.StartTimeout = TimeSpan.FromSeconds(10));

                x.AddConsumer<TestMessageConsumer>();

                x.UsingSqlServer((context, cfg) =>
                {
                    cfg.ReceiveEndpoint(Queue, e =>
                    {
                        e.ConfigureConsumeTopology = false;

                        e.ConfigureConsumer<TestMessageConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        await using (var connection = await provider.OpenTransport(TransportDialect.SqlServer))
        {
            var queue = await connection.QueueExists(TransportDialect.SqlServer, TransportSchema.Name, Queue, 1);
            var error = await connection.QueueExists(TransportDialect.SqlServer, TransportSchema.Name, Queue, 2);
            var deadLetter = await connection.QueueExists(TransportDialect.SqlServer, TransportSchema.Name, Queue, 3);

            Assert.Multiple(() =>
            {
                Assert.That(queue, Is.True, "the receive endpoint did not create its queue");
                Assert.That(error, Is.True, "the receive endpoint created no error queue, so a faulted message has nowhere to go");
                Assert.That(deadLetter, Is.True, "the receive endpoint created no dead letter queue");
            });
        }

        await harness.Stop();
    }

    /// <summary>A fresh queue per run: the transport database outlives a run.</summary>
    readonly string _queue = $"receive-endpoint-queue-{NewId.Next().ToString("N")}";

    string Queue => _queue;
}