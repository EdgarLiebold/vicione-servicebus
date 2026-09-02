namespace ViciOne.ServiceBus.BenchmarkConsole;

using System;
using BenchmarkDotNet.Attributes;
using JobService.Scheduling;


/// <summary>
/// The cost of walking a cron expression forward, moved here from a retired test case that measured a million iterations
/// with a stopwatch and wrote the result to the console. A throughput measurement has no pass or fail statement, so
/// it belongs in the benchmark tool and not in the required test run.
/// </summary>
[MemoryDiagnoser]
public class CronExpressionBenchmark
{
    static readonly DateTimeOffset _start = new(2012, 1, 1, 0, 0, 0, TimeSpan.Zero);

    CronExpression _everySecond;

    [Params(1000, 10000)]
    public int Occurrences { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _everySecond = new CronExpression("* * * * * ?");
    }

    [Benchmark]
    public DateTimeOffset? GetNextValidTimeAfter()
    {
        DateTimeOffset? next = _start;

        for (var index = 0; index < Occurrences; index++)
        {
            next = _everySecond.GetNextValidTimeAfter(next.Value);

            if (next is null)
                break;
        }

        return next;
    }
}
