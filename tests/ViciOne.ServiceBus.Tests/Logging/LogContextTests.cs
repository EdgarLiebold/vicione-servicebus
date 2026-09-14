using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "compiled-delegates-preserve-route-level-values-and-exception")]
    public void CompiledDelegates_PreserveTheirExactRouteLevelValuesAndException()
    {
        ILogContext? previous = LogContext.Current;
        var factory = new RecordingLoggerFactory(LogLevel.Trace);
        var exception = new InvalidOperationException("failure");

        try
        {
            LogContext.Current = new BusLogContext(factory);

            LogContext.Define<int>(LogLevel.Trace, "one {Value1}")(1, exception);
            LogContext.Define<int, string>(LogLevel.Debug, "two {Value1} {Value2}")(2, "two", exception);
            LogContext.Define<int, string, bool>(LogLevel.Information, "three {Value1} {Value2} {Value3}")(3, "three", true, exception);
            LogContext.Define<int, string, bool, decimal>(LogLevel.Warning, "four {Value1} {Value2} {Value3} {Value4}")(4, "four", false, 4.5m, exception);
            LogContext.Define<int, string, bool, decimal, Guid>(LogLevel.Error, "five {Value1} {Value2} {Value3} {Value4} {Value5}")
                (5, "five", true, 5.5m, Guid.Empty, exception);

            LogContext.DefineMessage<int, string>(LogLevel.Debug, "message two {Value1} {Value2}")(12, "twelve", exception);
            LogContext.DefineMessage<int, string, bool>(LogLevel.Information, "message three {Value1} {Value2} {Value3}")(13, "thirteen", false, exception);
            LogContext.DefineMessage<int, string, bool, decimal>(LogLevel.Warning, "message four {Value1} {Value2} {Value3} {Value4}")
                (14, "fourteen", true, 14.5m, exception);
            LogContext.DefineMessage<int, string, bool, decimal, Guid>(LogLevel.Critical, "message five {Value1} {Value2} {Value3} {Value4} {Value5}")
                (15, "fifteen", false, 15.5m, Guid.Empty, exception);

            Assert.Equal(
            [
                (ServiceBusLogCategories.Root, LogLevel.Trace, "one 1"),
                (ServiceBusLogCategories.Root, LogLevel.Debug, "two 2 two"),
                (ServiceBusLogCategories.Root, LogLevel.Information, "three 3 three True"),
                (ServiceBusLogCategories.Root, LogLevel.Warning, "four 4 four False 4.5"),
                (ServiceBusLogCategories.Root, LogLevel.Error, "five 5 five True 5.5 00000000-0000-0000-0000-000000000000"),
                (ServiceBusLogCategories.Messages, LogLevel.Debug, "message two 12 twelve"),
                (ServiceBusLogCategories.Messages, LogLevel.Information, "message three 13 thirteen False"),
                (ServiceBusLogCategories.Messages, LogLevel.Warning, "message four 14 fourteen True 14.5"),
                (ServiceBusLogCategories.Messages, LogLevel.Critical, "message five 15 fifteen False 15.5 00000000-0000-0000-0000-000000000000"),
            ], factory.Entries.Select(entry => (entry.Category, entry.Level, entry.Message)));
            Assert.All(factory.Entries, entry => Assert.Same(exception, entry.Exception));

            LogContext.Current = null;
            LogContext.Define<int>(LogLevel.Trace, "ignored {Value}")(99, exception);
            Assert.Equal(9, factory.Entries.Count);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "every-compiled-delegate-requires-a-template")]
    public void CompiledDelegateFactories_RejectAMissingTemplateForEveryArityAndRoute()
    {
        ArgumentNullException[] exceptions =
        [
            Assert.Throws<ArgumentNullException>(() => LogContext.Define<int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.Define<int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.Define<int, int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.Define<int, int, int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.Define<int, int, int, int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.DefineMessage<int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.DefineMessage<int, int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.DefineMessage<int, int, int, int>(LogLevel.Trace, null!)),
            Assert.Throws<ArgumentNullException>(() => LogContext.DefineMessage<int, int, int, int, int>(LogLevel.Trace, null!)),
        ];

        Assert.All(exceptions, exception => Assert.Equal("messageTemplate", exception.ParamName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "configuration-overloads-and-category-boundaries-are-explicit")]
    public void ConfigurationOverloads_InstallTheirExactLoggerAndValidateCategories()
    {
        ILogContext? previous = LogContext.Current;
        var factory = new RecordingLoggerFactory(LogLevel.Trace);

        try
        {
            LogContext.Current = null;
            ILogContext defaultChild = LogContext.CreateLogContext("Application.Default");
            Assert.Same(NullLogger.Instance, defaultChild.Logger);

            LogContext.ConfigureCurrentLogContext(factory);
            LogContext.Trace!.Log("trace");
            LogContext.Info!.Log("factory");
            LogContext.Critical!.Log("critical");

            ILogger singleLogger = factory.CreateLogger("Application.Single");
            LogContext.ConfigureCurrentLogContext(singleLogger);
            LogContext.Warning!.Log("single");

            LogContext.Current = new BusLogContext(factory);
            ILogContext child = LogContext.CreateLogContext("Application.Child");
            child.Error!.Log("child");

            Assert.Equal(
            [
                (ServiceBusLogCategories.Root, LogLevel.Trace, "trace"),
                (ServiceBusLogCategories.Root, LogLevel.Information, "factory"),
                (ServiceBusLogCategories.Root, LogLevel.Critical, "critical"),
                ("Application.Single", LogLevel.Warning, "single"),
                ("Application.Child", LogLevel.Error, "child"),
            ], factory.Entries.Select(entry => (entry.Category, entry.Level, entry.Message)));

            Assert.Throws<ArgumentNullException>(() => LogContext.ConfigureCurrentLogContext((ILogger)null!));
            Assert.Throws<ArgumentNullException>(() => LogContext.CreateLogContext(null!));
            Assert.Throws<ArgumentException>(() => LogContext.CreateLogContext(" "));

            LogContext.ConfigureCurrentLogContext((ILoggerFactory?)null);
            Assert.Same(NullLogger.Instance, LogContext.Current!.Logger);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "conditional-provider-configuration-preserves-usable-context")]
    public void ConditionalProviderConfiguration_ReplacesOnlyAbsentOrNullLoggingContexts()
    {
        ILogContext? previous = LogContext.Current;
        var replacementFactory = new RecordingLoggerFactory(LogLevel.Trace);
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(replacementFactory)
            .BuildServiceProvider();

        try
        {
            Assert.Throws<ArgumentNullException>(() => LogContext.ConfigureCurrentLogContextIfNull(null!));

            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            LogContext.Info!.Log("absent");

            LogContext.Current = new BusLogContext(NullLoggerFactory.Instance);
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            LogContext.Warning!.Log("null logger");

            var existingFactory = new RecordingLoggerFactory(LogLevel.Trace);
            var existing = new BusLogContext(existingFactory);
            LogContext.Current = existing;
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            LogContext.Error!.Log("existing");

            Assert.Same(existing, LogContext.Current);
            Assert.Equal(["absent", "null logger"], replacementFactory.Entries.Select(entry => entry.Message));
            Assert.Equal(["existing"], existingFactory.Entries.Select(entry => entry.Message));

            LogContext.Current = null;
            using ServiceProvider emptyProvider = new ServiceCollection().BuildServiceProvider();
            LogContext.ConfigureCurrentLogContextIfNull(emptyProvider);
            Assert.Same(NullLogger.Instance, LogContext.Current!.Logger);
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
