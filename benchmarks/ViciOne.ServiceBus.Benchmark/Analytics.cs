using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOneServiceBusBenchmark;

public static class Analytics
{
    public static double? Median<TColl, TValue>(this IEnumerable<TColl> source,
        Func<TColl, TValue> selector)
        where TValue : struct
    {
        return source.Select(selector).Median();
    }

    public static double? Percentile<TColl, TValue>(this IEnumerable<TColl> source,
        Func<TColl, TValue> selector, double percentile = 95)
        where TValue : struct
    {
        return source.Select(selector).Percentile(percentile);
    }

    public static double? Median<T>(this IEnumerable<T> source)
        where T : struct
    {
        return source.Percentile(50);
    }

    /// <summary>
    /// Calculates a linearly interpolated sample percentile using the same rank definition as the R-7
    /// quantile estimator: <c>(sampleCount - 1) * percentile / 100</c>.
    /// </summary>
    public static double? Percentile<T>(this IEnumerable<T> source, double percentile)
        where T : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (percentile is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Percentile must be between 0 and 100.");

        double[] samples = source.Select(value => Convert.ToDouble(value)).OrderBy(value => value).ToArray();
        if (samples.Length == 0)
            return null;

        var rank = (samples.Length - 1) * percentile / 100;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        if (lowerIndex == upperIndex)
            return samples[lowerIndex];

        var fraction = rank - lowerIndex;

        return samples[lowerIndex] + (samples[upperIndex] - samples[lowerIndex]) * fraction;
    }

    public static IReadOnlyList<HistogramBucket> Histogram(this IEnumerable<long> source, int segmentCount = 10)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (segmentCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, "Segment count must be positive.");

        long[] samples = source.ToArray();
        if (samples.Length == 0)
            return Array.Empty<HistogramBucket>();

        var minimum = samples.Min();
        var maximum = samples.Max();
        if (minimum == maximum)
            return new[] { new HistogramBucket(minimum, samples.Length) };

        var counts = new int[segmentCount];
        var span = (double)maximum - minimum;
        foreach (var sample in samples)
        {
            // The maximum computes an index of exactly segmentCount, which is one past the last
            // bucket, and the clamp is what puts it back into that bucket. Nothing is dropped.
            var index = Math.Clamp((int)(((double)sample - minimum) / span * segmentCount), 0, segmentCount - 1);

            counts[index]++;
        }

        return counts
            .Select((count, index) => new HistogramBucket(minimum + span * index / segmentCount, count))
            .Where(bucket => bucket.Count > 0)
            .ToArray();
    }


    public readonly struct HistogramBucket
    {
        public HistogramBucket(double lowerBoundTicks, int count)
        {
            LowerBoundTicks = lowerBoundTicks;
            Count = count;
        }

        public double LowerBoundTicks { get; }
        public int Count { get; }
    }
}
