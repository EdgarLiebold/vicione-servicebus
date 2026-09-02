using System.Diagnostics;
using System.Threading;
using ViciOneServiceBusBenchmark;

namespace ViciOne.ServiceBus.Benchmark.Tests;

internal sealed class BenchmarkTestClock : IBenchmarkMetricClock
{
    private long _elapsedTicks;

    public long ElapsedTicks => Volatile.Read(ref _elapsedTicks);

    public TimeSpan Elapsed => TimeSpan.FromSeconds((double)ElapsedTicks / Stopwatch.Frequency);

    public void Advance(long stopwatchTicks)
    {
        if (stopwatchTicks < 0)
            throw new ArgumentOutOfRangeException(nameof(stopwatchTicks));

        Interlocked.Add(ref _elapsedTicks, stopwatchTicks);
    }
}
