namespace ViciOneServiceBusBenchmark
{
    using System;
    using System.Diagnostics;


    /// <summary>
    /// Supplies the monotonic timestamps used by benchmark metric captures. The production implementation
    /// remains backed by <see cref="Stopwatch"/>; the seam exists so ordering can be proved without sleeping
    /// or asserting against machine-dependent elapsed time.
    /// </summary>
    internal interface IBenchmarkMetricClock
    {
        long ElapsedTicks { get; }
        TimeSpan Elapsed { get; }
    }


    internal sealed class StopwatchBenchmarkMetricClock :
        IBenchmarkMetricClock
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public long ElapsedTicks => _stopwatch.ElapsedTicks;
        public TimeSpan Elapsed => _stopwatch.Elapsed;
    }
}
