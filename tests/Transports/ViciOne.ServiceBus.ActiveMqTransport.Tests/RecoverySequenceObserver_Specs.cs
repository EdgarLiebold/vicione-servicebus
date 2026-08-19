#nullable enable
namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;


    /// <summary>
    /// When the receive endpoint has recovered, and when it only looks as though it had.
    /// <para>
    /// Recovery is a sequence. The two passing broker cases cannot tell these apart, because in a real
    /// outage the Fault always precedes the Ready: the order that has to be refused never occurs there,
    /// so it was never exercised. It is driven directly here.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Deciding_when_an_endpoint_has_recovered
    {
        [Test]
        public async Task Should_not_accept_a_ready_that_arrived_before_the_fault()
        {
            var observer = new RecoverySequenceObserver(Queue);
            observer.Watch();

            await observer.Ready(new EndpointReady());

            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the endpoint that never went away reported that it was ready, and that was read as a "
                + "recovery of an outage which had not even started");
        }

        [Test]
        public async Task Should_accept_a_ready_that_arrived_after_the_fault()
        {
            var observer = new RecoverySequenceObserver(Queue);
            observer.Watch();

            await observer.Faulted(new EndpointFaulted());
            await observer.Ready(new EndpointReady());

            Assert.That(await observer.Faulted(Budget), Is.True);
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.True);
        }

        [Test]
        public async Task Should_report_nothing_until_it_is_asked_to_watch()
        {
            var observer = new RecoverySequenceObserver(Queue);

            await observer.Faulted(new EndpointFaulted());

            Assert.That(await observer.Faulted(Budget), Is.False,
                "an observer that was never asked to watch an outage reported a fault, so a case could "
                + "read the endpoint's start as the outage it has not caused yet");
        }

        [Test]
        public async Task Should_ignore_everything_that_happened_before_the_watch_began()
        {
            var observer = new RecoverySequenceObserver(Queue);

            await observer.Faulted(new EndpointFaulted());
            await observer.Ready(new EndpointReady());

            observer.Watch();

            Assert.That(await observer.Faulted(Budget), Is.False,
                "a fault from before this outage would let the next Ready complete a recovery that "
                + "nothing caused");
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False);
        }

        [Test]
        public async Task Should_not_carry_a_fault_from_an_earlier_watch_into_the_next_one()
        {
            var observer = new RecoverySequenceObserver(Queue);

            observer.Watch();
            await observer.Faulted(new EndpointFaulted());

            observer.Watch();
            await observer.Ready(new EndpointReady());

            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the fault of the previous outage was still remembered, so the first Ready of the next "
                + "one completed a recovery from a fault that had already been recovered");
        }

        [Test]
        public async Task Should_not_report_a_recovery_when_only_the_fault_was_seen()
        {
            var observer = new RecoverySequenceObserver(Queue);
            observer.Watch();

            await observer.Faulted(new EndpointFaulted());

            Assert.That(await observer.Faulted(Budget), Is.True);
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the broker never came back, so nothing may report that it did");
        }


        [Test]
        public async Task Should_ignore_a_fault_from_another_endpoint_of_the_same_bus()
        {
            var observer = new RecoverySequenceObserver(Queue);
            observer.Watch();

            await observer.Faulted(new EndpointFaulted(SomebodyElse));

            Assert.That(await observer.Faulted(Budget), Is.False,
                "a fault of an endpoint this outage never touched was counted as the fault of the one "
                + "that was taken away");
        }

        [Test]
        public async Task Should_ignore_a_ready_from_another_endpoint_of_the_same_bus()
        {
            var observer = new RecoverySequenceObserver(Queue);
            observer.Watch();

            await observer.Faulted(new EndpointFaulted());
            await observer.Ready(new EndpointReady(SomebodyElse));

            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "an unrelated endpoint reported that it was ready, and that completed the recovery of "
                + "the endpoint that was still gone");
        }

        /// <summary>Short on purpose: every case here decides on a signal that is already present.</summary>
        static TimeSpan Budget => TimeSpan.FromMilliseconds(250);


        /// <summary>The endpoint these cases are about.</summary>
        const string Queue = "recovery-input";

        /// <summary>Another endpoint of the same bus, which every observer also hears from.</summary>
        const string SomebodyElse = "harness-input";


        sealed class EndpointReady :
            ReceiveEndpointReady
        {
            public EndpointReady(string endpoint = Queue)
            {
                InputAddress = new Uri($"activemq://127.0.0.1/{endpoint}");
            }

            public Uri InputAddress { get; }
            public IReceiveEndpoint ReceiveEndpoint => null!;
            public bool IsStarted => true;
        }


        sealed class EndpointFaulted :
            ReceiveEndpointFaulted
        {
            public EndpointFaulted(string endpoint = Queue)
            {
                InputAddress = new Uri($"activemq://127.0.0.1/{endpoint}");
            }

            public Uri InputAddress { get; }
            public IReceiveEndpoint ReceiveEndpoint => null!;
            public Exception? Exception => new InvalidOperationException("the broker was stopped");
        }
    }
}
