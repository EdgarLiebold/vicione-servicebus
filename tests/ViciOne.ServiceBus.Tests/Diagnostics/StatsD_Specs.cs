// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Diagnostics
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using Monitoring.Performance.StatsD;
    using NUnit.Framework;


    /// <summary>
    /// The StatsD counter writes one datagram per operation, and the datagram states the composed counter name and
    /// the StatsD metric type.
    ///
    /// The previous version pushed a hundred thousand increments at a hardcoded external host and printed the
    /// throughput, so it asserted nothing, could not run anywhere but on one machine, and measured the wall clock.
    /// The counter now sends to a receiver that this case owns, on the loopback address and on an ephemeral port
    /// that the receiver reports, and every datagram it produces is compared byte for byte.
    /// </summary>
    [TestFixture]
    public class StatsD_Specs
    {
        [Test]
        public void Should_send_one_datagram_per_counter_operation()
        {
            // Bound before the counter is constructed: the counter connects its socket in its constructor, and a
            // connected datagram socket whose target port is unbound reports the failure on a later send.
            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Parse(Loopback), 0));

            receiver.Client.ReceiveTimeout = ReceiveTimeoutMilliseconds;

            var port = ((IPEndPoint)receiver.Client.LocalEndPoint).Port;

            var counter = new StatsDPerformanceCounter(new StatsDConfiguration(Loopback, port), Category, Name, Instance);
            try
            {
                counter.Increment();
                counter.IncrementBy(42);
                counter.Set(7);

                // The three sends are not awaited by the product, so the arrival of the datagrams is the barrier.
                // They are compared as a set, because nothing in the product orders them.
                IReadOnlyList<string> datagrams = Receive(receiver, 3);

                Assert.That(datagrams, Is.EquivalentTo(new[]
                {
                    "test-category.test-counter.test-instance:1|c",
                    "test-category.test-counter.test-instance:42|c",
                    "test-category.test-counter.test-instance:7|g"
                }));
            }
            finally
            {
                // Disposed only after every datagram has arrived, because disposing closes the socket underneath
                // the sends the product did not await.
                counter.Dispose();
            }
        }

        /// <summary>
        /// The literal address on both sides, because a host name can resolve to the other address family and the
        /// datagram would then be sent to a socket nobody is listening on.
        /// </summary>
        const string Loopback = "127.0.0.1";

        const string Category = "test-category";
        const string Instance = "test-instance";
        const string Name = "test-counter";
        const int ReceiveTimeoutMilliseconds = 10000;

        static IReadOnlyList<string> Receive(UdpClient receiver, int count)
        {
            var datagrams = new List<string>(count);

            for (var index = 0; index < count; index++)
            {
                IPEndPoint sender = null;

                try
                {
                    datagrams.Add(Encoding.UTF8.GetString(receiver.Receive(ref sender)));
                }
                catch (SocketException)
                {
                    Assert.Fail($"Only {datagrams.Count} of {count} StatsD datagrams arrived at the receiver of this test");
                }
            }

            return datagrams;
        }
    }
}
