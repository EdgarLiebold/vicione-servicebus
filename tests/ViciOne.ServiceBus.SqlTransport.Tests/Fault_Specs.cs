namespace ViciOne.ServiceBus.DbTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using FaultMessages;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;
    using UnitTests;


    [TestFixture]
    public class When_a_consumer_throws_an_exception
    {
        /// <summary>
        /// An endpoint with no consumer for the message skips it, and a skipped message must not stay in the
        /// queue. The case used to send one and wait two seconds without asserting anything, so it passed
        /// whatever the transport did with it, including nothing.
        /// </summary>
        [Test]
        public async Task Should_dead_letter_skipped_messages()
        {
            // A fresh queue per run: the transport database outlives a single run, so a queue that keeps its
            // name carries the dead letters of every earlier one and no exact count can be asserted.
            var queue = $"skipped-message-queue-{NewId.Next().ToString("N")}";

            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(5), testTimeout: TimeSpan.FromSeconds(60));

                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ReceiveEndpoint(queue, e =>
                        {
                            e.PollingInterval = TimeSpan.FromMilliseconds(200);
                            e.ConfigureConsumeTopology = false;
                        });
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var endpoint = await harness.Bus.GetSendEndpoint(new Uri($"queue:{queue}"));

            await endpoint.Send(new TestMessage("nothing on this endpoint consumes this"));

            // The endpoint reports that nothing is arriving any more, rather than a fixed two seconds.
            await harness.InactivityTask;

            await using var connection = await provider.OpenTransport(TransportDialect.Postgres);

            var queued = await connection.DeliveryCount(TransportDialect.Postgres, TransportSchema.Name, queue, 1);
            var skipped = await connection.DeliveryCount(TransportDialect.Postgres, TransportSchema.Name, queue, 3);

            Assert.Multiple(() =>
            {
                Assert.That(queued, Is.Zero, "the skipped message is still in the queue it was sent to");
                Assert.That(skipped, Is.EqualTo(1), "the skipped message did not reach the dead letter queue");
            });

            await harness.Stop();
        }

        [Test]
        public async Task Should_publish_fault_and_move_to_the_error_queue()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddHandler(async (ConsumeContext<Fault<MemberUpdateCommand>> _) =>
                    {
                    });
                    x.AddHandler(async (ConsumeContext<UpdateMemberAddress> _) => throw new ApplicationException("I meant to do that!"));

                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await harness.Bus.Publish<UpdateMemberAddress>(new
            {
                MemberName = "Frank",
                Address = "123 American Way"
            });

            Assert.That(await harness.Consumed.Any<Fault<MemberUpdateCommand>>(), Is.True);

            await harness.Stop();
        }

        [Test]
        public async Task Should_use_built_in_redelivery_to_redeliver_faulted_messages()
        {
            // Three redeliveries a second apart, so the fault cannot arrive before three seconds have
            // passed. The inactivity timeout stood at two: the harness declared the run idle while its
            // own redelivery schedule was still running, and the case only passed while the machine was
            // quiet enough for the fault to slip in first. The timeout is bound to the schedule now.
            const int RedeliveryCount = 3;
            var redeliveryInterval = TimeSpan.FromSeconds(1);
            var inactivity = redeliveryInterval * (RedeliveryCount + 3);

            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.SetTestTimeouts(testInactivityTimeout: inactivity, testTimeout: TimeSpan.FromSeconds(60));
                    x.AddHandler(async (ConsumeContext<Fault<MemberUpdateCommand>> _) =>
                    {
                    });
                    x.AddHandler(async (ConsumeContext<UpdateMemberAddress> _) => throw new ApplicationException("I meant to do that!"));

                    x.AddConfigureEndpointsCallback((_, _, cfg) =>
                    {
                        cfg.UseDelayedRedelivery(r => r.Interval(RedeliveryCount, redeliveryInterval));
                    });

                    x.UsingPostgres((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await harness.Bus.Publish<UpdateMemberAddress>(new
            {
                MemberName = "Frank",
                Address = "123 American Way"
            });

            Assert.That(await harness.Consumed.Any<Fault<MemberUpdateCommand>>(), Is.True,
                "no fault arrived, so the redelivery schedule never ran out or the fault was never published");

            await harness.Stop();
        }
    }


    namespace FaultMessages
    {
        [ExcludeFromTopology]
        public interface ICommand
        {
        }


        public interface MemberUpdateCommand :
            ICommand
        {
            string MemberName { get; }
        }


        public interface UpdateMemberAddress :
            MemberUpdateCommand
        {
            string Address { get; }
        }
    }
}
