using System;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.JobService.Scheduling;

namespace ViciOne.ServiceBus.BenchmarkConsole;
/// <summary>
/// Measures the cost of advancing a cron expression over many iterations and reports throughput
/// without treating performance variation as a functional assertion.
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
