using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class TextWriterLoggerTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-LOGGER", "level-category-time-message-and-exception-format")]
    public void Logger_WritesTheExactEnabledEntryContract()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var options = new TextWriterLoggerOptions { MinimumLevel = LogLevel.Information };
        using var factory = new TextWriterLoggerFactory(writer, options, new FakeTimeProvider(ObservationTime));
        ILogger logger = factory.CreateLogger("Application.Payments");
        var formatterCalls = 0;

        logger.Log(LogLevel.Debug, default, "hidden", null, (state, _) =>
        {
            formatterCalls++;
            return state;
        });
        logger.Log(LogLevel.Information, default, "accepted", null, static (state, _) => state);
        logger.Log(LogLevel.Error, default, "failed", new InvalidOperationException("provider failure"), static (state, _) => state);

        string output = writer.ToString();
        Assert.Equal(0, formatterCalls);
        Assert.Contains("05:06:07.000 INF [Application.Payments] accepted", output, StringComparison.Ordinal);
        Assert.Contains("05:06:07.000 ERR [Application.Payments] failed", output, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), output, StringComparison.Ordinal);
        Assert.Contains("provider failure", output, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden", output, StringComparison.Ordinal);
        Assert.False(logger.IsEnabled(LogLevel.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-LOGGER", "all-level-codes-empty-messages-and-invalid-inputs")]
    public void Logger_CoversEveryLevelAndRejectsInvalidActiveInput()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var factory = new TextWriterLoggerFactory(
            writer,
            new TextWriterLoggerOptions(),
            new FakeTimeProvider(ObservationTime));
        ILogger logger = factory.CreateLogger("Level.Contract");

        logger.LogTrace("trace");
        logger.LogDebug("debug");
        logger.LogInformation("information");
        logger.LogWarning("warning");
        logger.LogError("error");
        logger.LogCritical("critical");
        logger.Log(LogLevel.Information, default, "ignored", null, static (_, _) => string.Empty);

        string output = writer.ToString();
        Assert.Contains("TRC [Level.Contract] trace", output, StringComparison.Ordinal);
        Assert.Contains("DBG [Level.Contract] debug", output, StringComparison.Ordinal);
        Assert.Contains("INF [Level.Contract] information", output, StringComparison.Ordinal);
        Assert.Contains("WRN [Level.Contract] warning", output, StringComparison.Ordinal);
        Assert.Contains("ERR [Level.Contract] error", output, StringComparison.Ordinal);
        Assert.Contains("CRT [Level.Contract] critical", output, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored", output, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() =>
            logger.Log(LogLevel.None, default, "ignored", null, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            logger.Log((LogLevel)42, default, "invalid", null, static (state, _) => state));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-LOGGER", "category-tree-suppression-is-case-insensitive-and-deduplicated")]
    public void SuppressCategory_DisablesTheCategoryTreeOnly()
    {
        var options = new TextWriterLoggerOptions();

        Assert.Same(options, options.SuppressCategory("Microsoft.Hosting"));
        Assert.Same(options, options.SuppressCategory("MICROSOFT.HOSTING"));
        using var factory = new TextWriterLoggerFactory(TextWriter.Null, options);

        Assert.Same(NullLogger.Instance, factory.CreateLogger("microsoft.hosting.Lifetime"));
        Assert.Same(NullLogger.Instance, factory.CreateLogger("Microsoft.Hosting"));
        Assert.IsType<TextWriterLogger>(factory.CreateLogger("Microsoft.Http"));
        Assert.IsType<TextWriterLogger>(factory.CreateLogger("Microsoft.HostingExtras"));
        Assert.Throws<ArgumentException>(() => options.SuppressCategory(" "));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-LOGGER", "factory-validates-inputs-and-preserves-writer-ownership")]
    public void Factory_RejectsInvalidInputsAndNeverDisposesTheCallerWriter()
    {
        var writer = new OwnershipTrackingWriter();
        var options = new TextWriterLoggerOptions();

        Assert.Throws<ArgumentNullException>(() => new TextWriterLoggerFactory(null!, options));
        Assert.Throws<ArgumentNullException>(() => new TextWriterLoggerFactory(writer, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextWriterLoggerFactory(
            writer,
            new TextWriterLoggerOptions { MinimumLevel = (LogLevel)42 }));
        Assert.Throws<ArgumentNullException>(() => new TextWriterLogger(
            null!,
            "category",
            LogLevel.Information));
        using (var factory = new TextWriterLoggerFactory(writer, options))
        {
            Assert.Throws<ArgumentNullException>(() => factory.CreateLogger(null!));
            Assert.Throws<ArgumentException>(() => factory.CreateLogger(" "));
            Assert.Throws<ArgumentException>(() => new TextWriterLogger(factory, " ", LogLevel.Information));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextWriterLogger(factory, "category", (LogLevel)42));
            Assert.Throws<ArgumentNullException>(() => factory.AddProvider(null!));
            Assert.Throws<NotSupportedException>(() => factory.AddProvider(new NullProvider()));
            IDisposable? scope = factory.CreateLogger("category").BeginScope("scope");
            Assert.NotNull(scope);
            scope.Dispose();
        }

        Assert.False(writer.WasDisposed);
        writer.Dispose();
        Assert.True(writer.WasDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-LOGGER", "concurrent-entries-are-serialized-at-the-writer-boundary")]
    public void ConcurrentWrites_AreSerializedIntoCompleteEntries()
    {
        using var writer = new ConcurrencyDetectingWriter();
        using var factory = new TextWriterLoggerFactory(writer, new TextWriterLoggerOptions());
        ILogger logger = factory.CreateLogger("Concurrent.Category");

        Parallel.For(0, 128, index =>
            logger.Log(LogLevel.Information, default, index, null, static (state, _) => state.ToString(CultureInfo.InvariantCulture)));

        Assert.Equal(128, writer.Lines.Count);
        Assert.All(writer.Lines, line => Assert.Contains("INF [Concurrent.Category]", line, StringComparison.Ordinal));
    }

    private sealed class NullProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose()
        {
        }
    }

    private sealed class OwnershipTrackingWriter : StringWriter
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ConcurrencyDetectingWriter : StringWriter
    {
        int _activeWriters;

        public ConcurrentQueue<string> Lines { get; } = new();

        public override void WriteLine(string? value)
        {
            if (Interlocked.Increment(ref _activeWriters) != 1)
                throw new InvalidOperationException("Concurrent writer access detected.");

            try
            {
                Thread.SpinWait(50_000);
                Lines.Enqueue(value ?? string.Empty);
            }
            finally
            {
                Interlocked.Decrement(ref _activeWriters);
            }
        }
    }
}
