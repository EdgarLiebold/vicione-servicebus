using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Xunit;
using ViciOne.ServiceBus.Advanced;

namespace LoggingLifetime;

public sealed class RecordingProvider : ILoggerProvider
{
    public readonly ConcurrentQueue<(string Category, string Text)> Entries = new();
    int _disposed;
    public int DisposeCount => Volatile.Read(ref _disposed);
    public ILogger CreateLogger(string categoryName) => new Recorder(this, categoryName);
    public void Dispose() => Interlocked.Increment(ref _disposed);
    public void Require(string category, string marker) => Assert.Single(Entries.Where(e => e.Category == category && e.Text == marker));
    sealed class Recorder(RecordingProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => owner.Entries.Enqueue((category, formatter(state, exception)));
    }
}

public sealed class CountingFactory(ILoggerFactory inner) : ILoggerFactory
{
    int _disposed;
    public int DisposeCount => Volatile.Read(ref _disposed);
    public ILogger CreateLogger(string categoryName) => inner.CreateLogger(categoryName);
    public void AddProvider(ILoggerProvider provider) => inner.AddProvider(provider);
    public void Dispose() { Interlocked.Increment(ref _disposed); inner.Dispose(); }
}

public sealed class FaultProvider(IServiceProvider inner, Exception fault) : IServiceProvider
{
    public object? GetService(Type serviceType) => serviceType == typeof(ILoggerFactory) ? throw fault : inner.GetService(serviceType);
}

public sealed class FaultMeterFactory(IMeterFactory inner, Exception fault) : IMeterFactory
{
    public int Attempts;
    public Meter Create(MeterOptions options) { Interlocked.Increment(ref Attempts); throw fault; }
    public void Dispose() { _ = inner; }
}

public sealed class MeterProbe : IDisposable
{
    readonly MeterListener _listener = new();
    public readonly ConcurrentQueue<Instrument> Measurements = new();
    public readonly ConcurrentQueue<Instrument> Published = new();
    public MeterProbe()
    {
        _listener.InstrumentPublished = (instrument, listener) => { Published.Enqueue(instrument); listener.EnableMeasurementEvents(instrument); };
        _listener.SetMeasurementEventCallback<long>((instrument, _, _, _) => Measurements.Enqueue(instrument));
        _listener.SetMeasurementEventCallback<double>((instrument, _, _, _) => Measurements.Enqueue(instrument));
        _listener.Start();
    }
    public void Dispose() => _listener.Dispose();
}

public static class Markers
{
    public static void Emit(string marker)
    {
        var context = Assert.IsAssignableFrom<ViciOne.ServiceBus.Logging.ILogContext>(LogContext.Current);
        context.Logger.LogInformation(marker + ".root");
        LogContext.CreateLogContext("Public.Category").Logger.LogInformation(marker + ".category");
        context.Messages.Logger.LogInformation(marker + ".messages");
    }
    public static void Require(RecordingProvider recorder, string marker, string category = "Public.Category")
    {
        Assert.Single(recorder.Entries.Where(e => e.Text == marker + ".root"));
        recorder.Require(category, marker + ".category");
        Assert.Single(recorder.Entries.Where(e => e.Text == marker + ".messages"));
    }
}
