// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;


    /// <summary>
    /// The timeline of the retained test framework renders the message flow of a conversation: which message was
    /// published, which was sent, which consumer handled it, and at which endpoint.
    ///
    /// The previous version printed that rendering into the output stream of the test runner and asserted nothing,
    /// and it paid the full inactivity timeout because it never forced the timeline to close. The rendering is now
    /// written into a writer of this test and the structure of the flow is asserted: the number of each operation
    /// follows from the topology of the consumers, not from timing.
    /// </summary>
    [TestFixture]
    public class MessageFlow_Specs
    {
        [Test]
        public async Task Should_generate_a_graph_of_the_message_flow()
        {
            var harness = new InMemoryTestHarness
            {
                TestTimeout = TimeSpan.FromSeconds(30),
                TestInactivityTimeout = TimeSpan.FromSeconds(3)
            };

            harness.Consumer(() => new AFooConsumer());
            harness.Consumer(() => new BFooConsumer());
            harness.Consumer(() => new CFooConsumer());
            harness.Consumer(() => new DFooConsumer());
            harness.Consumer(() => new EFooConsumer());

            EndpointConvention.Map<EFoo>(harness.InputQueueAddress);

            await harness.Start();
            try
            {
                await harness.Bus.Publish<AFoo>(new { InVar.CorrelationId });

                await harness.Bus.Publish<BFoo>(new { InVar.CorrelationId });

                // The flow is closed exactly when the last leaf message has been consumed. Nine of them follow from
                // the topology, so this is the barrier that replaces waiting for the inactivity timeout.
                Assert.That(await harness.Consumed.SelectAsync<DFoo>().Take(ExpectedDFoo).Count(), Is.EqualTo(ExpectedDFoo));

                using var timeline = new StringWriter();

                await harness.OutputTimeline(timeline, options => options.Now().IncludeAddress());

                var rendered = timeline.ToString();

                string[] lines = rendered.Split('\n');

                int Rows(string operation)
                {
                    return lines.Count(line => line.Contains(operation));
                }

                Assert.Multiple(() =>
                {
                    Assert.That(Rows("Publish AFoo"), Is.EqualTo(1), rendered);
                    Assert.That(Rows("Publish BFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Publish CFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Send EFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Publish DFoo"), Is.EqualTo(ExpectedDFoo), rendered);

                    Assert.That(Rows("Consume AFoo"), Is.EqualTo(1), rendered);
                    Assert.That(Rows("Consume BFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Consume CFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Consume EFoo"), Is.EqualTo(3), rendered);
                    Assert.That(Rows("Consume DFoo"), Is.EqualTo(ExpectedDFoo), rendered);

                    Assert.That(lines.Where(line => line.Contains("Consume ")), Is.All.Contains(InputQueueName),
                        "Every consumed message must be reported at the endpoint that handled it");
                });
            }
            finally
            {
                await harness.Stop();

                harness.Dispose();
            }
        }

        /// <summary>
        /// Two published roots produce three BFoo, each of which produces one CFoo and sends one EFoo. Each CFoo
        /// produces two DFoo and each EFoo produces one, which is six plus three.
        /// </summary>
        const int ExpectedDFoo = 9;

        const string InputQueueName = "input_queue";
    }


    public interface ICorrelated
    {
        public Guid CorrelationId { get; set; }
    }


    public interface AFoo : ICorrelated
    {
    }


    public interface BFoo : ICorrelated
    {
    }


    public interface CFoo : ICorrelated
    {
    }


    public interface DFoo : ICorrelated
    {
    }


    public interface EFoo : ICorrelated
    {
    }


    public class AFooConsumer : IConsumer<AFoo>
    {
        public async Task Consume(ConsumeContext<AFoo> context)
        {
            await context.Publish<BFoo>(new {CorrelationId = Guid.NewGuid()});

            await context.Publish<BFoo>(new {CorrelationId = Guid.NewGuid()});
        }
    }


    public class BFooConsumer : IConsumer<BFoo>
    {
        public async Task Consume(ConsumeContext<BFoo> context)
        {
            await context.Publish<CFoo>(new {CorrelationId = Guid.NewGuid()});

            await context.Send<EFoo>(new {CorrelationId = Guid.NewGuid()});
        }
    }


    public class CFooConsumer : IConsumer<CFoo>
    {
        public async Task Consume(ConsumeContext<CFoo> context)
        {
            await context.Publish<DFoo>(new {CorrelationId = Guid.NewGuid()});

            await context.Publish<DFoo>(new {CorrelationId = Guid.NewGuid()});
        }
    }


    public class DFooConsumer : IConsumer<DFoo>
    {
        public Task Consume(ConsumeContext<DFoo> context)
        {
            return Task.CompletedTask;
        }
    }


    public class EFooConsumer : IConsumer<EFoo>
    {
        public async Task Consume(ConsumeContext<EFoo> context)
        {
            await context.Publish<DFoo>(new {CorrelationId = Guid.NewGuid()});
        }
    }
}
