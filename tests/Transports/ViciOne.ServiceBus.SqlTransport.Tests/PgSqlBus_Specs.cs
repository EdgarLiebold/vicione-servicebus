namespace ViciOne.ServiceBus.DbTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;
    using UnitTests;


    [TestFixture]
    public class Configuring_the_postgresql_bus
    {

        // Three throughput cases stood here: they published or sent a thousand messages, printed a
        // message rate and asserted nothing, which is why they were permanently excluded. The
        // measurement lives in benchmarks/ViciOne.ServiceBus.Benchmark, which addresses the same
        // PostgreSQL transport through SqlOptionSet, so nothing was lost by removing them.
        [Test]
        public async Task Should_support_standard_syntax_with_consumers()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<TestMessageConsumer>();
                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(2));

                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ReceiveEndpoint("input-queue", e =>
                        {
                            e.ConfigureConsumeTopology = false;
                            e.PrefetchCount = 30;

                            e.ConfigureConsumer<TestMessageConsumer>(context);
                        });
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:input-queue"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });

            Assert.That(await harness.Consumed.Any<TestMessage>(), Is.True);

            IReceivedMessage<TestMessage> context = harness.Consumed.Select<TestMessage>().Single();

            Assert.Multiple(() =>
            {
                Assert.That(context.Context.MessageId, Is.Not.Null);
                Assert.That(context.Context.ConversationId, Is.Not.Null);
                Assert.That(context.Context.DestinationAddress, Is.Not.Null);
                Assert.That(context.Context.SourceAddress, Is.Not.Null);
            });

            await harness.Stop();
        }

        [Test]
        public async Task Should_support_standard_syntax_with_consumers_and_topology()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<TestMessageConsumer>();
                    x.AddConsumer<NestedMessageConsumer>();
                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(2));

                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await harness.Bus.Publish(new MessageC(NewId.NextGuid()));

            Assert.That(await harness.Consumed.Any<MessageA>(), Is.True);
        }

        [Test]
        /// <summary>
        /// The short configuration form sends a message that actually arrives. The case sent one and
        /// asserted nothing, so a send that reached no queue would have passed it.
        /// </summary>
        public async Task Should_support_the_standard_syntax()
        {
            var queue = $"standard-syntax-queue-{NewId.Next().ToString("N")}";

            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(3));
                    x.UsingPostgres();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var endpoint = await harness.Bus.GetSendEndpoint(new Uri($"queue:{queue}"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });

            await using var connection = await provider.OpenTransport(TransportDialect.Postgres);

            Assert.That(await connection.DeliveryCount(TransportDialect.Postgres, TransportSchema.Name, queue, 1),
                Is.EqualTo(1), "the message the short syntax sent did not reach its queue");

            await harness.Stop();
        }

        [Test]
        /// <summary>
        /// Three queues, three messages, each in its own queue. The case sent all three and asserted
        /// nothing, so one queue swallowing all of them, or none of them arriving, would have passed.
        /// </summary>
        public async Task Should_support_the_standard_syntax_with_three_queues()
        {
            var run = NewId.Next().ToString("N");
            string[] queues =
            {
                $"three-queues-a-{run}",
                $"three-queues-b-{run}",
                $"three-queues-c-{run}"
            };

            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(3));
                    x.UsingPostgres();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            foreach (var queue in queues)
            {
                var endpoint = await harness.Bus.GetSendEndpoint(new Uri($"queue:{queue}"));

                await endpoint.Send(new TestMessage($"Hello, {queue}!"), x =>
                {
                    x.Headers.Set("Simple-Header", "Some Value");
                });
            }

            await using var connection = await provider.OpenTransport(TransportDialect.Postgres);

            var delivered = new List<long>();
            foreach (var queue in queues)
                delivered.Add(await connection.DeliveryCount(TransportDialect.Postgres, TransportSchema.Name, queue, 1));

            Assert.That(delivered, Is.EqualTo(new long[] { 1, 1, 1 }),
                "the three messages are not one in each of the three queues");

            await harness.Stop();
        }
    }


    namespace UnitTests
    {
        using System;


        public record TestMessage
        {
            public TestMessage(Guid Id, string Value)
            {
                this.Id = Id;
                this.Value = Value;
            }

            public TestMessage(string Value)
            {
                Id = NewId.NextGuid();
                this.Value = Value;
            }

            public TestMessage()
            {
            }

            public Guid Id { get; init; }
            public string Value { get; init; }
        }


        public record TestMultipleMessage
        {
            public TestMultipleMessage(Guid Id, string Value)
            {
                this.Id = Id;
                this.Value = Value;
            }

            public TestMultipleMessage(string Value)
            {
                Id = NewId.NextGuid();
                this.Value = Value;
            }

            public TestMultipleMessage()
            {
            }

            public Guid Id { get; init; }
            public string Value { get; init; }
        }


        public record SlowMessage;


        public record MessageA(Guid CorrelationId);


        public record MessageB(Guid CorrelationId) :
            MessageA(CorrelationId);


        public record MessageC(Guid CorrelationId) :
            MessageB(CorrelationId);


        public class TestMessageConsumer :
            IConsumer<TestMessage>
        {
            public async Task Consume(ConsumeContext<TestMessage> context)
            {
            }
        }


        public class TestMultipleMessageConsumer :
            IConsumer<TestMultipleMessage>
        {
            public async Task Consume(ConsumeContext<TestMultipleMessage> context)
            {
            }
        }


        public class SlowMessageConsumer :
            IConsumer<SlowMessage>
        {
            public async Task Consume(ConsumeContext<SlowMessage> context)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), context.CancellationToken);

                LogContext.Info?.Log("Consumed the message");
            }
        }


        public class NestedMessageConsumer :
            IConsumer<MessageA>
        {
            public async Task Consume(ConsumeContext<MessageA> context)
            {
            }
        }
    }
}
