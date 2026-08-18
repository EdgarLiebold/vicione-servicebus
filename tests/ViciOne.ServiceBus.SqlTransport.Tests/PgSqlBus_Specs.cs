namespace ViciOne.ServiceBus.DbTransport.Tests
{
    using System;
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
        public async Task Should_support_the_standard_syntax()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.UsingPostgres();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:input-queue"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });
        }

        [Test]
        public async Task Should_support_the_standard_syntax_with_three_queues()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.UsingPostgres();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:input-queue"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });

            endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:input-queue-2"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });

            endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:input-queue-3"));

            await endpoint.Send(new TestMessage("Hello, World!"), x =>
            {
                x.Headers.Set("Simple-Header", "Some Value");
            });
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
