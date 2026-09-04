using System.Diagnostics;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark;
using Xunit;
using LatencyMetricCapture = ViciOneServiceBusBenchmark.Latency.MessageMetricCapture;
using RequestMetricCapture = ViciOneServiceBusBenchmark.RequestResponse.MessageMetricCapture;

namespace ViciOne.ServiceBus.Benchmark.Tests;

public sealed class AnalyticsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "percentile-linear-interpolation")]
    public void Percentile_UsesLinearInterpolationForSmallEvenSample()
    {
        double result = new[] { 1, 2, 3, 4 }.Percentile(95)!.Value;

        Assert.InRange(result, 3.8499999, 3.8500001);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "median-shares-quantile-definition")]
    public void Median_UsesTheSameQuantileDefinition()
    {
        Assert.Equal(2.5, new[] { 1, 2, 3, 4 }.Median());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "single-sample-percentile")]
    public void Percentile_WithOneSample_ReturnsThatSample()
    {
        Assert.Equal(42, new[] { 42L }.Percentile(95));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "empty-sample-percentile")]
    public void Percentile_WithEmptySample_ReturnsNull()
    {
        Assert.Null(Array.Empty<long>().Percentile(95));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "percentile-closed-range")]
    public void Percentile_OutsideClosedRange_IsRejected(double percentile)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1L }.Percentile(percentile));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "histogram-includes-maximum")]
    public void Histogram_IncludesMaximumValueAndEverySample()
    {
        IReadOnlyList<Analytics.HistogramBucket> histogram = new long[] { 0, 5, 10 }.Histogram(2);

        Assert.Equal(3, histogram.Sum(bucket => bucket.Count));
        Assert.Equal(2, histogram[^1].Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "identical-samples-single-bucket")]
    public void Histogram_WithIdenticalSamples_ProducesOneCompleteBucket()
    {
        IReadOnlyList<Analytics.HistogramBucket> histogram = new long[] { 7, 7, 7 }.Histogram();

        Analytics.HistogramBucket bucket = Assert.Single(histogram);
        Assert.Equal(7, bucket.LowerBoundTicks);
        Assert.Equal(3, bucket.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "extreme-histogram-does-not-overflow")]
    public void Histogram_WithExtremeValues_DoesNotOverflow()
    {
        IReadOnlyList<Analytics.HistogramBucket> histogram =
            new[] { long.MinValue, long.MaxValue - 1, long.MaxValue }.Histogram(2);

        Assert.Equal(3, histogram.Sum(bucket => bucket.Count));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "histogram-segment-count-positive")]
    public void Histogram_WithNonPositiveSegmentCount_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1L }.Histogram(0));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "adaptive-stopwatch-unit")]
    public void FormatStopwatchTicks_UsesAnAdaptiveUnitWithoutRoundingToZero()
    {
        string result = BenchmarkReporting.FormatStopwatchTicks(Stopwatch.Frequency / 10_000_000d);

        Assert.Equal("100.000 ns", result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "latency-capture-starts-before-send")]
    public async Task LatencyCapture_StartsBeforeTheSendDelegateIsInvokedAsync()
    {
        var clock = new BenchmarkTestClock();
        var capture = new LatencyMetricCapture(1, clock);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () =>
        {
            clock.Advance(137);
            return Task.CompletedTask;
        });
        clock.Advance(23);
        await ((ViciOneServiceBusBenchmark.Latency.IReportConsumerMetric)capture).ConsumedAsync<object>(messageId);

        var metric = Assert.Single(capture.GetMessageMetrics());
        Assert.Equal(137, metric.SendCompletionLatency);
        Assert.Equal(160, metric.ConsumeLatency);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "request-capture-starts-before-delegate")]
    public async Task RequestCapture_StartsBeforeTheRequestDelegateIsInvokedAsync()
    {
        var clock = new BenchmarkTestClock();
        var capture = new RequestMetricCapture(1, clock);
        var messageId = Guid.NewGuid();

        await capture.ResponseReceivedAsync(messageId, () =>
        {
            clock.Advance(211);
            return Task.FromResult(new object());
        });
        clock.Advance(34);
        await ((ViciOneServiceBusBenchmark.RequestResponse.IReportConsumerMetric)capture).ConsumedAsync<object>(messageId);

        var metric = Assert.Single(capture.GetMessageMetrics());
        Assert.Equal(211, metric.RequestLatency);
        Assert.Equal(245, metric.ConsumeLatency);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ANALYTICS", "completion-continuations-are-asynchronous")]
    public void MetricCompletionTasks_RunContinuationsAsynchronously()
    {
        var latency = new LatencyMetricCapture(1);
        var request = new RequestMetricCapture(1);

        Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously,
            latency.SendCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously,
            latency.ConsumeCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously,
            request.RequestCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously,
            request.ConsumeCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
