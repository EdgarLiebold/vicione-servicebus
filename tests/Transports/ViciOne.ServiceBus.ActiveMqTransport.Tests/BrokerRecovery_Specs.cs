namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;
    using Transports;


    /// <summary>
    /// The broker really goes away, the transport notices, the broker really comes back and delivery
    /// resumes over the address the bus was configured with.
    /// <para>
    /// Three observations, in that order, each read from the transport rather than assumed. Delivery
    /// works before the outage. The receive endpoint reports that it faulted while the broker was
    /// stopped and that it is ready again after the broker returned - from the endpoint observer, not
    /// from this case's belief that something must have happened. Only then a message carrying an
    /// identity this run owns is published, and it has to arrive exactly once.
    /// </para>
    /// <para>
    /// Nothing is published while the broker is down. Measured on this transport: a publish issued in
    /// that state does not return - not the task, the call - because it resolves a send endpoint
    /// against a session whose broker is not answering.
    /// </para>
    /// <para>
    /// What makes the address hold is the relay in front of the broker. A restarted container is
    /// published on a new ephemeral port every time, so without the relay the bus would be reconnecting
    /// to a port nobody listens on and a green case would be measuring the fixture. The bus is built and
    /// torn down inside the case rather than by a shared fixture, because a harness that outlives the
    /// broker it was started against blocks in its own teardown.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Recovering_from_a_broker_outage
    {
        [TestCase(ActiveMqHostAddress.ActiveMqScheme, TestName = "OpenWire")]
        [TestCase(ActiveMqHostAddress.AmqpScheme, TestName = "AMQP")]
        public async Task Should_deliver_again_after_the_broker_came_back(string protocol)
        {
            if (!BrokerOutageClient.Available)
            {
                Assert.Fail($"{BrokerOutageClient.ControlVariable} is not set, so this run cannot take the "
                    + "broker away. Start the fixture with --allow-broker-outage activemq.");
            }

            var queue = $"recovery-input-{NewId.Next().ToString("N")}";
            var received = new ConcurrentQueue<string>();
            var observer = new RecoverySequenceObserver(queue);

            var harness = new ActiveMqTestHarness(protocol, queue);
            harness.OnConfigureActiveMqReceiveEndpoint += configurator =>
                configurator.Handler<RecoveryMessage>(context =>
                {
                    received.Enqueue(context.Message.Value);

                    return Task.CompletedTask;
                });
            harness.OnConnectObservers += bus => bus.ConnectReceiveEndpointObserver(observer);

            await harness.Start();
            try
            {
                var beforeTheOutage = NewId.NextGuid().ToString();

                await harness.Bus.Publish(new RecoveryMessage { Value = beforeTheOutage });

                Assert.That(await Arrived(received, beforeTheOutage), Is.EqualTo(1),
                    "delivery did not work before the outage, so nothing after it would mean anything");

                observer.Watch();

                try
                {
                    await BrokerOutageClient.Interrupt();

                    Assert.That(await observer.Faulted(FaultBudget), Is.True,
                        "the receive endpoint never reported a fault while the broker was stopped, so "
                        + "nothing it did afterwards can be called a recovery");
                }
                finally
                {
                    // Unconditionally, and outside anything this case can cancel: a broker left stopped
                    // is a broker every later case blocks against.
                    await BrokerOutageClient.Restore();
                }

                Assert.That(await observer.ReadyAfterTheFault(ReadyBudget), Is.True,
                    "the receive endpoint never became ready again after the broker returned");

                var afterTheOutage = NewId.NextGuid().ToString();

                await harness.Bus.Publish(new RecoveryMessage { Value = afterTheOutage });

                Assert.That(await Arrived(received, afterTheOutage), Is.EqualTo(1),
                    "the message published after the recovery did not arrive exactly once over the address "
                    + "the bus was configured with");
            }
            finally
            {
                await StopWithoutHanging(harness);
            }
        }

        /// <summary>
        /// How often that exact identity arrived, counted once the endpoint has stopped receiving.
        /// <para>
        /// Exactly once is the assertion, and counting lets a duplicate fail on its own sentence
        /// instead of hiding behind "at least one arrived". The count is taken after a quiet period
        /// rather than a fixed pause: the observation ends when nothing has arrived for
        /// <see cref="QuietPeriod"/>, which is a statement about the endpoint rather than a guess at
        /// how long a second copy would take. Nothing beyond that period is claimed.
        /// </para>
        /// </summary>
        static async Task<int> Arrived(ConcurrentQueue<string> received, string identity)
        {
            var elapsed = Stopwatch.StartNew();

            while (elapsed.Elapsed < DeliveryBudget)
            {
                if (received.Any(value => value == identity))
                    break;

                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            // Drain: keep watching until the endpoint has been quiet, so a second copy that is still
            // in flight is part of the count rather than of the next case.
            var quiet = Stopwatch.StartNew();
            var lastSeen = received.Count;
            while (quiet.Elapsed < QuietPeriod && elapsed.Elapsed < DeliveryBudget + QuietPeriod)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                var now = received.Count;
                if (now != lastSeen)
                {
                    lastSeen = now;
                    quiet.Restart();
                }
            }

            return received.Count(value => value == identity);
        }

        /// <summary>How long the endpoint has to have received nothing before the count is taken.</summary>
        static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Stops the harness within a bound, and fails if it cannot.
        /// <para>
        /// The broker behind the relay is a different process than the one this harness connected to,
        /// so a teardown that cleans entities it created can wait for an answer that is not coming.
        /// That is a defect worth reporting, not a note: a harness that will not stop leaves a
        /// connection and a consumer behind for every later case in the run.
        /// </para>
        /// <para>
        /// The completed task is compared with the stop task itself. Comparing it with
        /// Task.CompletedTask compares against a different object entirely, so the branch was taken
        /// whenever the stop had not finished synchronously - which is always - and the case reported
        /// a failure it had not measured while ignoring the one it had.
        /// </para>
        /// </summary>
        static async Task StopWithoutHanging(ActiveMqTestHarness harness)
        {
            try
            {
                Task stopping = harness.Stop();

                Task finished = await Task.WhenAny(stopping, Task.Delay(StopBudget));

                Assert.That(finished, Is.SameAs(stopping),
                    $"the harness did not stop within {StopBudget.TotalSeconds:0} s, so it leaves its "
                    + "connection and its consumer behind for everything after it");

                // Awaited on the successful path so an exception during the stop is observed rather
                // than swallowed into an unobserved task.
                await stopping;
            }
            finally
            {
                harness.Dispose();
            }
        }

        /// <summary>How long the transport is given to notice that the broker is gone.</summary>
        static readonly TimeSpan FaultBudget = TimeSpan.FromSeconds(90);

        /// <summary>How long the transport is given to become ready again once the broker is back.</summary>
        static readonly TimeSpan ReadyBudget = TimeSpan.FromSeconds(120);

        /// <summary>How long one message is given to arrive.</summary>
        static readonly TimeSpan DeliveryBudget = TimeSpan.FromSeconds(60);

        /// <summary>How long the teardown is given before the case stops waiting for it.</summary>
        static readonly TimeSpan StopBudget = TimeSpan.FromSeconds(30);


        public class RecoveryMessage
        {
            public string Value { get; set; }
        }
    }
}
