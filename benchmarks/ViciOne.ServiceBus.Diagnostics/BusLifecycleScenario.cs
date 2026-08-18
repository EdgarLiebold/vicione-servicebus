namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing;


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
    public static async Task<object> Run(int cycles, int sampleEvery, CancellationToken cancellationToken)
    {
        await RunScopedBroker.CreateVirtualHost("test", cancellationToken);

        var process = Process.GetCurrentProcess();
        var samples = new List<object>();

        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var harness = new RabbitMqTestHarness();
            var roundTrip = Stopwatch.StartNew();

            await harness.Start();
            try
            {
                // The handler is connected to the started bus, so the message has to reach the bus
                // endpoint and come back out of it. A send that is merely accepted proves nothing.
                Task<ConsumeContext<DiagnosticPing>> handled = harness.SubscribeHandler<DiagnosticPing>();

                await harness.BusSendEndpoint.Send(new DiagnosticPing());

                await handled.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            finally
            {
                await harness.Stop();
                roundTrip.Stop();
            }

            if (cycle == 1 || cycle % sampleEvery == 0 || cycle == cycles)
            {
                process.Refresh();
                samples.Add(new
                {
                    cycle,
                    roundTripMilliseconds = roundTrip.ElapsedMilliseconds,
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
