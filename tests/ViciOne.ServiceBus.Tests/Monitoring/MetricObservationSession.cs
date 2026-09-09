using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using ViciOne.ServiceBus.Monitoring;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

internal sealed class MetricObservationSession : IDisposable
{
    private readonly ConcurrentQueue<MetricInstrumentDescriptor> _instruments = new();
    private readonly ConcurrentQueue<MetricMeasurement> _measurements = new();
    private readonly MeterListener _listener = new();
    private readonly SemaphoreSlim _measurementAvailable = new(0);

    public MetricObservationSession(object? meterScope)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name != ServiceBusTelemetry.MeterName
                || !ReferenceEquals(instrument.Meter.Scope, meterScope))
                return;

            _instruments.Enqueue(new MetricInstrumentDescriptor(
                instrument.Name,
                instrument.GetType(),
                instrument.Unit,
                instrument.Description,
                instrument.Meter.Version,
                instrument is Histogram<double> histogram
                    ? histogram.Advice?.HistogramBucketBoundaries?.ToArray()
                    : null));
            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            Record(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            Record(instrument, value, tags));
        _listener.Start();
    }

    public IReadOnlyList<MetricInstrumentDescriptor> Instruments => _instruments.ToArray();

    public IReadOnlyList<MetricMeasurement> Measurements => _measurements.ToArray();

    public void RecordObservableInstruments() => _listener.RecordObservableInstruments();

    public async Task WaitForCountAsync(
        Func<MetricMeasurement, bool> predicate,
        int expectedCount,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedCount);

        while (_measurements.Count(predicate) < expectedCount)
        {
            if (!await _measurementAvailable.WaitAsync(timeout, cancellationToken))
            {
                throw new TimeoutException(
                    $"Expected {expectedCount} matching measurements, but observed {_measurements.Count(predicate)}.");
            }
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
        _measurementAvailable.Dispose();
    }

    private void Record<T>(
        Instrument instrument,
        T value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
        where T : struct
    {
        var copy = new KeyValuePair<string, object?>[tags.Length];
        tags.CopyTo(copy);
        _measurements.Enqueue(new MetricMeasurement(
            instrument.Name,
            Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
            copy));
        _measurementAvailable.Release();
    }
}

internal sealed record MetricInstrumentDescriptor(
    string Name,
    Type InstrumentType,
    string? Unit,
    string? Description,
    string? SourceVersion,
    IReadOnlyList<double>? HistogramBucketBoundaries);

internal sealed record MetricMeasurement(
    string Name,
    double Value,
    IReadOnlyList<KeyValuePair<string, object?>> Tags)
{
    public object? Tag(string name) => Assert.Single(Tags, tag => tag.Key == name).Value;
}
