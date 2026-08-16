namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ViciOneServiceBusBenchmark;
using LatencyMetricCapture = ViciOneServiceBusBenchmark.Latency.MessageMetricCapture;
using RequestMetricCapture = ViciOneServiceBusBenchmark.RequestResponse.MessageMetricCapture;


public class AnalyticsTests
{
    [Test]
    public void Percentile_UsesLinearInterpolationForSmallEvenSample()
    {
        var result = new[] { 1, 2, 3, 4 }.Percentile(95);

        Assert.That(result, Is.EqualTo(3.85).Within(0.0000001));
    }

    [Test]
    public void Median_UsesTheSameQuantileDefinition()
    {
        var result = new[] { 1, 2, 3, 4 }.Median();

        Assert.That(result, Is.EqualTo(2.5));
    }

    [Test]
    public void Percentile_WithOneSample_ReturnsThatSample()
    {
        Assert.That(new[] { 42L }.Percentile(95), Is.EqualTo(42));
    }

    [Test]
    public void Percentile_WithEmptySample_ReturnsNull()
    {
        Assert.That(Array.Empty<long>().Percentile(95), Is.Null);
    }

    [TestCase(-0.01)]
    [TestCase(100.01)]
    public void Percentile_OutsideClosedRange_IsRejected(double percentile)
    {
        Assert.That(() => new[] { 1L }.Percentile(percentile), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Histogram_IncludesMaximumValueAndEverySample()
    {
        var histogram = new long[] { 0, 5, 10 }.Histogram(2);

        Assert.That(histogram.Sum(bucket => bucket.Count), Is.EqualTo(3));
        Assert.That(histogram[^1].Count, Is.EqualTo(2));
    }

    [Test]
    public void Histogram_WithIdenticalSamples_ProducesOneCompleteBucket()
    {
        var histogram = new long[] { 7, 7, 7 }.Histogram();

        Assert.That(histogram, Has.Count.EqualTo(1));
        Assert.That(histogram[0].LowerBoundTicks, Is.EqualTo(7));
        Assert.That(histogram[0].Count, Is.EqualTo(3));
    }

    [Test]
    public void Histogram_WithExtremeValues_DoesNotOverflow()
    {
        var histogram = new[] { long.MinValue, long.MaxValue - 1, long.MaxValue }.Histogram(2);

        Assert.That(histogram.Sum(bucket => bucket.Count), Is.EqualTo(3));
    }

    [Test]
    public void Histogram_WithNonPositiveSegmentCount_IsRejected()
    {
        Assert.That(() => new[] { 1L }.Histogram(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FormatStopwatchTicks_UsesAnAdaptiveUnitWithoutRoundingToZero()
    {
        var result = BenchmarkReporting.FormatStopwatchTicks(Stopwatch.Frequency / 10_000_000d);

        Assert.That(result, Is.EqualTo("100.000 ns"));
    }

    [Test]
    public async Task LatencyCapture_StartsBeforeTheSendDelegateIsInvoked()
    {
        var capture = new LatencyMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, () =>
        {
            System.Threading.Thread.Sleep(10);
            return Task.CompletedTask;
        });
        await ((ViciOneServiceBusBenchmark.Latency.IReportConsumerMetric)capture).Consumed<object>(messageId);

        Assert.That(capture.GetMessageMetrics().Single().SendCompletionLatency,
            Is.GreaterThan(Stopwatch.Frequency / 200));
    }

    [Test]
    public async Task RequestCapture_StartsBeforeTheRequestDelegateIsInvoked()
    {
        var capture = new RequestMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.ResponseReceived(messageId, () =>
        {
            System.Threading.Thread.Sleep(10);
            return Task.FromResult(new object());
        });
        await ((ViciOneServiceBusBenchmark.RequestResponse.IReportConsumerMetric)capture).Consumed<object>(messageId);

        Assert.That(capture.GetMessageMetrics().Single().RequestLatency,
            Is.GreaterThan(Stopwatch.Frequency / 200));
    }

    [Test]
    public void MetricCompletionTasks_RunContinuationsAsynchronously()
    {
        var latency = new LatencyMetricCapture(1);
        var request = new RequestMetricCapture(1);

        Assert.Multiple(() =>
        {
            Assert.That(latency.SendCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously,
                Is.EqualTo(TaskCreationOptions.RunContinuationsAsynchronously));
            Assert.That(latency.ConsumeCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously,
                Is.EqualTo(TaskCreationOptions.RunContinuationsAsynchronously));
            Assert.That(request.RequestCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously,
                Is.EqualTo(TaskCreationOptions.RunContinuationsAsynchronously));
            Assert.That(request.ConsumeCompleted.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously,
                Is.EqualTo(TaskCreationOptions.RunContinuationsAsynchronously));
        });
    }
}
