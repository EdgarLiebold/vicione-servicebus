using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMq.Testing;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.Diagnostics;
/// <summary>
/// Starts and stops a bus repeatedly and records what one round trip costs as the cycles accumulate.
/// <para>
/// The question is whether something survives a bus lifecycle that should not: a round trip that grows
/// with the cycle count, a thread that is never returned, work items that queue up. Each of those is a
/// number here rather than a verdict, because the scenario observes and does not judge.
/// </para>
/// </summary>
static class BusLifecycleScenario
{
    public static async Task<object> RunAsync(int cycles, int sampleEvery, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHostAsync("test", cancellationToken);

        using var process = Process.GetCurrentProcess();
        var samples = new List<object>();

        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Three separate measurements, because they answer three different questions. Reporting
            // their sum as a round trip would name the start and the stop as latency.
            var harness = new RabbitMqTestHarness();
            var startElapsed = Stopwatch.StartNew();
            var started = false;
            long roundTripMilliseconds = -1;
            long stopMilliseconds;

            try
            {
                await harness.StartAsync(cancellationToken: cancellationToken);
                started = true;
                startElapsed.Stop();

                // The handler is connected to the started bus, so the message has to reach the bus
                // endpoint and come back out of it. A send that is merely accepted proves nothing.
                var roundTrip = Stopwatch.StartNew();

                Task<ConsumeContext<DiagnosticPing>> handled = harness.SubscribeHandlerAsync<DiagnosticPing>(cancellationToken: cancellationToken);

                await harness.BusSendEndpoint.SendAsync(new DiagnosticPing(), cancellationToken: cancellationToken);

                await handled.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

                roundTripMilliseconds = roundTrip.ElapsedMilliseconds;
            }
            finally
            {
                // A start that threw still leaves a harness to dispose, and skipping that is how a
                // diagnostic starts measuring its own leak.
                var stopElapsed = Stopwatch.StartNew();
                if (started)
                    await harness.StopAsync(cancellationToken: cancellationToken);

                harness.Dispose();
                stopMilliseconds = stopElapsed.ElapsedMilliseconds;
            }

            if (cycle == 1 || cycle % sampleEvery == 0 || cycle == cycles)
            {
                process.Refresh();
                samples.Add(new
                {
                    cycle,
                    startMilliseconds = startElapsed.ElapsedMilliseconds,
                    roundTripMilliseconds,
                    stopMilliseconds,
                    cycleMilliseconds = startElapsed.ElapsedMilliseconds + roundTripMilliseconds + stopMilliseconds,
                    threadPoolThreads = ThreadPool.ThreadCount,
                    processThreads = process.Threads.Count,
                    pendingWorkItems = ThreadPool.PendingWorkItemCount,
                    managedMemoryMegabytes = GC.GetTotalMemory(false) / (1024 * 1024)
                });
            }
        }

        return new { scenario = "bus-lifecycle", cycles, sampleEvery, samples };
    }


    public record DiagnosticPing;
}
