namespace ViciOneServiceBusBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;


    public static class BenchmarkReporting
    {
        public static string FormatStopwatchTicks(double ticks)
        {
            if (ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(ticks), ticks, "Elapsed stopwatch ticks cannot be negative.");

            var seconds = ticks / Stopwatch.Frequency;
            var milliseconds = seconds * 1000;
            if (milliseconds >= 1)
                return $"{milliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms";

            var microseconds = seconds * 1_000_000;
            if (microseconds >= 1)
                return $"{microseconds.ToString("F3", CultureInfo.InvariantCulture)} us";

            var nanoseconds = seconds * 1_000_000_000;

            return $"{nanoseconds.ToString("F3", CultureInfo.InvariantCulture)} ns";
        }

        public static void WriteLatencySummary<T>(string label, IReadOnlyCollection<T> metrics, Func<T, long> selector)
        {
            if (metrics == null)
                throw new ArgumentNullException(nameof(metrics));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (metrics.Count == 0)
                throw new ArgumentException("At least one metric is required.", nameof(metrics));

            long[] samples = metrics.Select(selector).ToArray();

            Console.WriteLine("{0} sample count: {1}", label, samples.Length);
            Console.WriteLine("Average {0}: {1}", label, FormatStopwatchTicks(samples.Average()));
            Console.WriteLine("Minimum {0}: {1}", label, FormatStopwatchTicks(samples.Min()));
            Console.WriteLine("Maximum {0}: {1}", label, FormatStopwatchTicks(samples.Max()));
            Console.WriteLine("Median {0}: {1}", label, FormatStopwatchTicks(samples.Median().Value));
            Console.WriteLine("P95 {0}: {1}", label, FormatStopwatchTicks(samples.Percentile(95).Value));
        }

        public static void WriteHistogram<T>(string label, IReadOnlyCollection<T> metrics, Func<T, long> selector)
        {
            if (metrics == null)
                throw new ArgumentNullException(nameof(metrics));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (metrics.Count == 0)
                throw new ArgumentException("At least one metric is required.", nameof(metrics));

            long[] samples = metrics.Select(selector).ToArray();
            IReadOnlyList<Analytics.HistogramBucket> histogram = samples.Histogram();
            var maximumCount = histogram.Max(bucket => bucket.Count);

            Console.WriteLine("{0} distribution (samples: {1})", label, samples.Length);
            foreach (var bucket in histogram)
            {
                var barLength = bucket.Count * 60 / maximumCount;
                Console.WriteLine("{0,14} {1,-60} ({2,7})", FormatStopwatchTicks(bucket.LowerBoundTicks),
                    new string('*', barLength), bucket.Count);
            }
        }
    }
}
