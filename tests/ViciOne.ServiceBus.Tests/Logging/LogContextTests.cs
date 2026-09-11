using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Logging;

public sealed class LogContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "each-convenience-writer-preserves-its-exact-log-level")]
    public void EnabledWriters_MapToTheirExactMicrosoftLogLevels()
    {
        var factory = new RecordingLoggerFactory(LogLevel.Trace);
        var context = new BusLogContext(factory);

        context.Trace!.Log("trace");
        context.Debug!.Log("debug");
        context.Info!.Log("information");
        context.Warning!.Log("warning");
        context.Error!.Log(new InvalidOperationException("error"), "error");
        context.Critical!.Log("critical");

        Assert.Equal(
        [
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical,
        ], factory.Entries.Select(entry => entry.Level));
        Assert.All(factory.Entries, entry => Assert.Equal(ServiceBusLogCategories.Root, entry.Category));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "categories-and-disabled-levels-are-preserved")]
    public void Contexts_PreserveMessageAndChildCategoriesAndHideDisabledLevels()
    {
        var factory = new RecordingLoggerFactory(LogLevel.Warning);
        var context = new BusLogContext(factory);

        Assert.Null(context.Trace);
        Assert.Null(context.Debug);
        Assert.Null(context.Info);
        Assert.NotNull(context.Warning);
        context.Messages.Warning!.Log("message");
        context.CreateLogContext("Application.Payments").Error!.Log("child");

        Assert.Equal(
        [
            (ServiceBusLogCategories.Messages, LogLevel.Warning),
            ("Application.Payments", LogLevel.Error),
        ], factory.Entries.Select(entry => (entry.Category, entry.Level)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "invalid-adapter-inputs-fail-at-the-boundary")]
    public void LoggingAdapters_RejectInvalidInputsAndUnsupportedProviders()
    {
        Assert.Throws<ArgumentNullException>(() => new BusLogContext(null!));
        Assert.Throws<ArgumentNullException>(() => new SingleLoggerFactory(null!));

        var context = new BusLogContext(new RecordingLoggerFactory(LogLevel.Trace));
        Assert.Throws<ArgumentException>(() => context.CreateLogContext(" "));
        Assert.Throws<ArgumentNullException>(() => context.Info!.Log(null!));
        Assert.Throws<ArgumentNullException>(() => context.Info!.Log(null!, "message"));

        var factory = new SingleLoggerFactory(NullLogger.Instance);
        Assert.Throws<ArgumentNullException>(() => factory.CreateLogger(null!));
        Assert.Throws<ArgumentNullException>(() => factory.AddProvider(null!));
        Assert.Throws<NotSupportedException>(() => factory.AddProvider(new NullProvider()));
        factory.Dispose();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "set-if-null-accepts-absent-candidate-and-preserves-current")]
    public void SetCurrentIfNull_AcceptsAnAbsentCandidateAndNeverReplacesAnExistingContext()
    {
        ILogContext? previous = LogContext.Current;
        var existing = new BusLogContext(NullLoggerFactory.Instance);
        var candidate = new BusLogContext(NullLoggerFactory.Instance);

        try
        {
            LogContext.Current = existing;

            LogContext.SetCurrentIfNull(null);
            LogContext.SetCurrentIfNull(candidate);

            Assert.Same(existing, LogContext.Current);

            LogContext.Current = null;
            LogContext.SetCurrentIfNull(null);
            Assert.Null(LogContext.Current);

            LogContext.SetCurrentIfNull(candidate);
            Assert.Same(candidate, LogContext.Current);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    private sealed class RecordingLoggerFactory(LogLevel minimumLevel) : ILoggerFactory
    {
        public List<LogEntry> Entries { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, minimumLevel, Entries);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(string category, LogLevel minimumLevel, List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(new LogEntry(category, logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class NullProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose()
        {
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
}
