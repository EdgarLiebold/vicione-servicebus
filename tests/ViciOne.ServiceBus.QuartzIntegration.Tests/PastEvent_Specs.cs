// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.QuartzIntegration.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Scheduling;


    [TestFixture]
    public class Specifying_an_event_in_the_past :
        QuartzInMemoryTestFixture
    {
        [SetUp]
        public Task Reset_the_scheduler_clock()
        {
            return ResetTime();
        }

        [TearDown]
        public Task Put_the_scheduler_clock_back()
        {
            return ResetTime();
        }

        [Test]
        public async Task Should_be_able_to_cancel_a_future_event()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();
            Task<ConsumeContext<Horizon>> horizon = SubscribeHandler<Horizon>();

            ScheduledMessage<A> scheduledMessage =
                await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow + DueTime, new A { Name = "Joe" });

            // The schedule exists: the scheduler consumed the request, not just the transport.
            Assert.That(await InMemoryTestHarness.Consumed.Any<ScheduleMessage>(
                    x => x.Exception == null && x.Context.Message.CorrelationId == scheduledMessage.TokenId, TestCancellationToken),
                Is.True, "The scheduler did not accept the future event");

            // A horizon message beyond the due time of the canceled event. Its arrival proves the scheduler has
            // worked through that point in time, so the absent delivery below is a statement and not a guess.
            await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow + BeyondDueTime, new Horizon());

            await Scheduler.CancelScheduledSend(scheduledMessage);

            Assert.That(await InMemoryTestHarness.Consumed.Any<CancelScheduledMessage>(
                    x => x.Exception == null && x.Context.Message.TokenId == scheduledMessage.TokenId, TestCancellationToken),
                Is.True, "The cancel was not consumed successfully by the scheduler");

            await AdvanceTime(BeyondDueTime);

            await horizon.WaitAsync(TestCancellationToken);

            Assert.That(handler.Status, Is.EqualTo(TaskStatus.WaitingForActivation),
                "The canceled event must not be delivered, not even after its due time has passed");
        }

        [Test]
        public async Task Should_handle_now_properly()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();

            await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow, new A { Name = "Joe" });

            await handler;
        }

        [Test]
        public async Task Should_handle_slightly_in_the_future_properly()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();

            await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow + TimeSpan.FromSeconds(2), new A { Name = "Joe" });

            await handler;
        }

        [Test]
        public async Task Should_include_message_headers()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();

            var requestId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();
            var initiatorId = Guid.NewGuid();
            await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow, new A { Name = "Joe" }, Pipe.Execute<SendContext>(x =>
            {
                x.FaultAddress = Bus.Address;
                x.ResponseAddress = InputQueueAddress;
                x.RequestId = requestId;
                x.CorrelationId = correlationId;
                x.ConversationId = conversationId;
                x.InitiatorId = initiatorId;

                x.Headers.Set("Hello", "World");
            }));

            ConsumeContext<A> context = await handler;

            Assert.Multiple(() =>
            {
                Assert.That(context.FaultAddress, Is.EqualTo(Bus.Address));
                Assert.That(context.ResponseAddress, Is.EqualTo(InputQueueAddress));
                Assert.That(context.RequestId.HasValue, Is.True);
                Assert.That(context.RequestId.Value, Is.EqualTo(requestId));
                Assert.That(context.CorrelationId.HasValue, Is.True);
                Assert.That(context.CorrelationId.Value, Is.EqualTo(correlationId));
                Assert.That(context.ConversationId.HasValue, Is.True);
                Assert.That(context.ConversationId.Value, Is.EqualTo(conversationId));
                Assert.That(context.InitiatorId.HasValue, Is.True);
                Assert.That(context.InitiatorId.Value, Is.EqualTo(initiatorId));

                Assert.That(context.Headers.TryGetHeader("Hello", out var value), Is.True);
                Assert.That(value, Is.EqualTo("World"));
            });
        }

        [Test]
        public async Task Should_properly_send_the_message()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();

            await Scheduler.ScheduleSend(Bus.Address, DateTime.UtcNow + TimeSpan.FromHours(-1), new A { Name = "Joe" });

            await handler;
        }


        static readonly TimeSpan DueTime = TimeSpan.FromSeconds(120);
        static readonly TimeSpan BeyondDueTime = TimeSpan.FromSeconds(180);


        class A
        {
            public string Name { get; set; }
        }


        public class Horizon
        {
        }
    }


    [TestFixture]
    public class Specifying_an_event_reschedule_if_exists :
        QuartzInMemoryTestFixture
    {
        [Test]
        public async Task Should_reschedule()
        {
            Task<ConsumeContext<A>> handler = SubscribeHandler<A>();
            var id = NewId.NextGuid();
            var expected = "Joe 2";
            await Scheduler.ScheduleSend(Bus.Address, TimeSpan.FromSeconds(120), new A
            {
                Id = id,
                Name = "Joe"
            });

            await Task.Delay(2000);

            await Scheduler.ScheduleSend(Bus.Address, TimeSpan.FromSeconds(5), new A
            {
                Id = id,
                Name = expected
            });

            ConsumeContext<A> result = await handler;
            Assert.That(result.Message.Name, Is.EqualTo(expected));
        }

        public Specifying_an_event_reschedule_if_exists()
        {
            ScheduleTokenId.UseTokenId<A>(x => x.Id);
        }


        class A
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
