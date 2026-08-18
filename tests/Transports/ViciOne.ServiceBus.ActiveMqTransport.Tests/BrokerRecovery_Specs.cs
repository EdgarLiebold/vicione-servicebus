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
            if (!BrokerFaultController.Available)
            {
                Assert.Fail($"{BrokerFaultController.ControlVariable} is not set, so this run cannot take the "
                    + "broker away. Start the fixture with --allow-broker-outage activemq.");
            }

            var queue = $"recovery-input-{NewId.Next().ToString("N")}";
            var received = new ConcurrentQueue<string>();
            var observer = new EndpointStateObserver();

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

                observer.Rearm();

                try
                {
                    await BrokerFaultController.Interrupt();

                    Assert.That(await observer.Faulted(FaultBudget), Is.True,
                        "the receive endpoint never reported a fault while the broker was stopped, so "
                        + "nothing it did afterwards can be called a recovery");
                }
                finally
                {
                    // Unconditionally, and outside anything this case can cancel: a broker left stopped
                    // is a broker every later case blocks against.
                    await BrokerFaultController.Restore();
                }

                Assert.That(await observer.ReadyAgain(ReadyBudget), Is.True,
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
        /// How often that exact identity arrived. Exactly once is the assertion; counting lets a
        /// duplicate fail on its own sentence instead of hiding behind "at least one arrived".
        /// </summary>
        static async Task<int> Arrived(ConcurrentQueue<string> received, string identity)
        {
            var elapsed = Stopwatch.StartNew();

            while (elapsed.Elapsed < DeliveryBudget)
            {
                if (received.Any(value => value == identity))
                {
                    // A moment for a second copy to show up, so a duplicate is reported as one.
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    return received.Count(value => value == identity);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            return 0;
        }

        /// <summary>
        /// Stops the harness, and gives up rather than hanging the run.
        /// <para>
        /// The broker behind the relay is a different process than the one this harness connected to,
        /// so a teardown that tries to clean entities it created can wait for an answer that is not
        /// coming. The case has already made its statement at this point; a teardown that will not
        /// finish must not turn that statement into a run without an end.
        /// </para>
        /// </summary>
        static async Task StopWithoutHanging(ActiveMqTestHarness harness)
        {
            if (await Task.WhenAny(harness.Stop(), Task.Delay(StopBudget)) != Task.CompletedTask)
                TestContext.Out.WriteLine($"the harness did not stop within {StopBudget.TotalSeconds:0} s");

            harness.Dispose();
        }

        /// <summary>How long the transport is given to notice that the broker is gone.</summary>
        static readonly TimeSpan FaultBudget = TimeSpan.FromSeconds(90);

        /// <summary>How long the transport is given to become ready again once the broker is back.</summary>
        static readonly TimeSpan ReadyBudget = TimeSpan.FromSeconds(120);

        /// <summary>How long one message is given to arrive.</summary>
        static readonly TimeSpan DeliveryBudget = TimeSpan.FromSeconds(60);

        /// <summary>How long the teardown is given before the case stops waiting for it.</summary>
        static readonly TimeSpan StopBudget = TimeSpan.FromSeconds(30);


        /// <summary>
        /// What the receive endpoint said about itself. Both signals are latched, so one that arrives a
        /// moment before the wait starts still counts.
        /// </summary>
        class EndpointStateObserver :
            IReceiveEndpointObserver
        {
            TaskCompletionSource<bool> _faulted = Fresh();
            TaskCompletionSource<bool> _ready = Fresh();

            /// <summary>Forgets the readiness of the start, so only the one after the outage counts.</summary>
            public void Rearm()
            {
                Volatile.Write(ref _faulted, Fresh());
                Volatile.Write(ref _ready, Fresh());
            }

            public Task<bool> Faulted(TimeSpan budget)
            {
                return Within(Volatile.Read(ref _faulted).Task, budget);
            }

            public Task<bool> ReadyAgain(TimeSpan budget)
            {
                return Within(Volatile.Read(ref _ready).Task, budget);
            }

            static async Task<bool> Within(Task<bool> signal, TimeSpan budget)
            {
                return await Task.WhenAny(signal, Task.Delay(budget)) == signal;
            }

            static TaskCompletionSource<bool> Fresh()
            {
                return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public Task Ready(ReceiveEndpointReady ready)
            {
                Volatile.Read(ref _ready).TrySetResult(true);

                return Task.CompletedTask;
            }

            public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

            public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

            public Task Faulted(ReceiveEndpointFaulted faulted)
            {
                Volatile.Read(ref _faulted).TrySetResult(true);

                return Task.CompletedTask;
            }
        }


        public class RecoveryMessage
        {
            public string Value { get; set; }
        }
    }
}
