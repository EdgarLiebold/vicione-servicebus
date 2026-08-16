namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;
    using Testing;


    /// <summary>
    /// Diagnostic probe for the accumulation observed across a full suite run.
    ///
    /// The full RabbitMQ category fails only from roughly position 229 of 288 onwards, always with the
    /// 30 second test timeout rather than the 6 second inactivity timeout, while the broker ends the
    /// run with zero connections and zero channels. That points at a process-local effect across bus
    /// lifecycles rather than at the broker or at the individual tests.
    ///
    /// This probe starts and stops a bus repeatedly and records, at a fixed interval, how long a
    /// single publish/consume round trip takes plus the process resources at that moment. It is
    /// explicit so it never runs as part of a category; run it deliberately:
    ///
    ///   --filter "FullyQualifiedName~BusLifecycleAccumulation_Probe"
    /// </summary>
    [TestFixture]
    [Explicit("Diagnostic probe, run deliberately")]
    public class BusLifecycleAccumulation_Probe
    {
        const int Cycles = 240;
        const int SampleEvery = 20;

        [Test]
        public async Task Measure_round_trip_across_bus_lifecycles()
        {
            var process = Process.GetCurrentProcess();

            TestContext.Out.WriteLine(
                "cycle;roundTripMs;threadPoolThreads;processThreads;pendingWorkItems;managedMemoryMb");

            for (var cycle = 1; cycle <= Cycles; cycle++)
            {
                var harness = new RabbitMqTestHarness();
                var roundTrip = Stopwatch.StartNew();

                await harness.Start();
                try
                {
                    // SubscribeHandler connects to the bus consume pipe, so it needs a started bus
                    // and the message has to reach the bus endpoint.
                    var handled = harness.SubscribeHandler<PingMessage>();

                    await harness.BusSendEndpoint.Send(new PingMessage());

                    await handled.WaitAsync(TimeSpan.FromSeconds(30));
                }
                finally
                {
                    await harness.Stop();
                    roundTrip.Stop();
                }

                if (cycle % SampleEvery == 0 || cycle == 1)
                {
                    process.Refresh();
                    TestContext.Out.WriteLine(
                        $"{cycle};{roundTrip.ElapsedMilliseconds};{ThreadPool.ThreadCount};"
                        + $"{process.Threads.Count};{ThreadPool.PendingWorkItemCount};"
                        + $"{GC.GetTotalMemory(false) / (1024 * 1024)}");
                }
            }
        }
    }
}
