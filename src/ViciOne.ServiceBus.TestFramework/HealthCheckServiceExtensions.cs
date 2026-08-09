// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using NUnit.Framework;


    public static class HealthCheckServiceExtensions
    {
        /// <summary>How long a single status transition may take. Unchanged from the imported baseline.</summary>
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);


        public static async Task WaitForHealthStatus(this HealthCheckService healthChecks, HealthStatus expectedStatus)
        {
            var timer = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource(Timeout);

            HealthReport report = default;
            try
            {
                do
                {
                    report = await healthChecks.CheckHealthAsync(cts.Token);

                    if (report.Status == expectedStatus)
                        break;

                    // The budget is unchanged; only the sampling rate is. A one second pause meant a
                    // caller could wait almost a full second after the status had already flipped, and
                    // the kill switch spec calls this four times in a row. Sampling faster can hide
                    // nothing: a status that never arrives still expires.
                    await Task.Delay(50, cts.Token);
                }
                while (report.Status != expectedStatus);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // A bare TaskCanceledException says nothing about what actually happened. Report the
                // status that was reached and the entries that did not, so the next failure is a
                // finding instead of a guess.
                var entries = report.Entries == null
                    ? "no report was produced"
                    : string.Join(", ", report.Entries.Select(x => $"{x.Key}={x.Value.Status}"));

                Assert.Fail($"The health status stayed at '{report.Status}' instead of reaching "
                    + $"'{expectedStatus}' within {Timeout.TotalSeconds:0} s. Entries: {entries}.");
            }

            await TestContext.Out.WriteLineAsync(report.ToJsonString());

            await TestContext.Out.WriteLineAsync(
                $"reached {expectedStatus} after {timer.ElapsedMilliseconds} ms");

            Assert.That(report.Status, Is.EqualTo(expectedStatus));
        }
    }
}
