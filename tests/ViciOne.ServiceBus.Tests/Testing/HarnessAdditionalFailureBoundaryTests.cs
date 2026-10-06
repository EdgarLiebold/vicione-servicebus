using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class HarnessAdditionalFailureBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "dispose-all-owned-observation-after-callback-fault")]
    public async Task AsyncHarness_DisposeRetiresInactivityDespiteTestCancellationFailureAsync(bool callbackThrows)
    {
        var clock = new FakeTimeProvider();
        var harness = new ProbeHarness(clock)
        {
            TestTimeout = TimeSpan.FromHours(1),
            TestInactivityTimeout = TimeSpan.FromMinutes(1),
        };
        var callbackFailure = new IOException("unique test-scope cancellation callback failure");
        CancellationToken testToken = harness.TestCancellationToken;
        CancellationToken inactivityToken = harness.InactivityToken;
        Task inactivity = harness.InactivityTask;
        int callbackCalls = 0;
        Exception? thrownCallback = null;
        using CancellationTokenRegistration registration = testToken.Register(() =>
        {
            callbackCalls++;
            if (callbackThrows)
            {
                thrownCallback = callbackFailure;
                throw callbackFailure;
            }
        });
        try
        {
            Exception? failure = Record.Exception(() => harness.Dispose());
            Assert.Equal(1, callbackCalls);
            Assert.True(testToken.IsCancellationRequested);
            Assert.Equal(callbackThrows ? callbackFailure : null, thrownCallback);
            // This observation precedes fallback clock advancement and proves public retirement.
            Assert.True(inactivityToken.IsCancellationRequested);
            if (callbackThrows)
            {
                Assert.NotNull(failure);
                Assert.Single(ExceptionTree(failure), cause => ReferenceEquals(cause, callbackFailure));
            }
            else
                Assert.Null(failure);
            Assert.Throws<ObjectDisposedException>(() => harness.TestCancellationToken);
            Assert.Null(Record.Exception(() => harness.Dispose()));
            Assert.Equal(1, callbackCalls);
            await inactivity.WaitAsync(OperationTimeout(), CancellationToken.None);
        }
        finally
        {
            // Original early-exit disposal leaves the public observation active; finish it independently.
            clock.Advance(TimeSpan.FromMinutes(1));
            try { await inactivity.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
            finally { harness.Dispose(); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "failed-start-rollback-error-logger-preserves-primary")]
    public async Task BusHarness_StartRollbackPreservesPrimaryDespiteOptionalErrorLoggerAsync(bool loggerThrows)
    {
        var primary = new InvalidOperationException("unique harness observer admission failure");
        var rollback = new ApplicationException("unique failed-start rollback failure");
        var loggerFailure = new IOException("unique failed-start error diagnostic failure");
        const string template = "Stopping the bus after a failed harness start also faulted";
        var logger = new ErrorLogger(template, loggerThrows ? loggerFailure : null);
        ILogContext? previous = LogContext.Current;
        IBusControl bus = DispatchProxy.Create<IBusControl, BusProxy>();
        var proxy = (BusProxy)(object)bus;
        proxy.StopFailure = rollback;
        var harness = new FailingHarness(bus, primary) { TestTimeout = OperationTimeout() };
        Task? start = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            start = harness.StartAsync(TestContext.Current.CancellationToken);
            Exception? failure = await Record.ExceptionAsync(() => start.WaitAsync(OperationTimeout(), CancellationToken.None));
            ErrorLogger.Entry entry = Assert.Single(logger.Entries, item => item.Template == template);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(rollback, entry.Exception);
            Assert.Equal(1, proxy.StopCalls);
            Assert.True(proxy.StopToken.CanBeCanceled);
            Assert.False(proxy.StopToken.IsCancellationRequested);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            Assert.Equal(loggerThrows ? loggerFailure : null, logger.ThrownFailure);
            Assert.Same(primary, failure);
            Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        }
        finally
        {
            try
            {
                if (start is not null)
                    await JoinExpectedAsync(start, primary, loggerFailure);
            }
            finally
            {
                try { await harness.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
                finally { LogContext.Current = previous; }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "started-bus-stop-error-logger-preserves-primary")]
    public async Task BusHarness_StopPreservesPrimaryDespiteOptionalErrorLoggerAsync(bool loggerThrows)
    {
        var primary = new ApplicationException("unique actually-started bus stop failure");
        var loggerFailure = new IOException("unique bus stop error diagnostic failure");
        const string template = "Stop bus faulted";
        var logger = new ErrorLogger(template, loggerThrows ? loggerFailure : null);
        ILogContext? previous = LogContext.Current;
        var harness = new ForwardingHarness { TestTimeout = OperationTimeout(), TestInactivityTimeout = OperationTimeout() };
        Task? start = null;
        Task? stop = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            start = harness.StartAsync(TestContext.Current.CancellationToken);
            await start.WaitAsync(OperationTimeout(), CancellationToken.None);
            Assert.NotNull(harness.Actual);
            Assert.NotNull(harness.Proxy);
            Assert.Equal(0, harness.Proxy.StopCalls);
            harness.Proxy.StopFailure = primary;
            stop = harness.StopAsync(TestContext.Current.CancellationToken);
            Exception? failure = await Record.ExceptionAsync(() => stop.WaitAsync(OperationTimeout(), CancellationToken.None));
            ErrorLogger.Entry entry = Assert.Single(logger.Entries, item => item.Template == template);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(primary, entry.Exception);
            Assert.Equal(1, harness.Proxy.StopCalls);
            Assert.True(harness.Proxy.StopToken.CanBeCanceled);
            Assert.False(harness.Proxy.StopToken.IsCancellationRequested);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            Assert.Equal(loggerThrows ? loggerFailure : null, logger.ThrownFailure);
            Assert.Same(primary, failure);
            Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        }
        finally
        {
            try
            {
                if (start is not null)
                    await start.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            finally
            {
                try
                {
                    if (stop is not null)
                        await JoinExpectedAsync(stop, primary, loggerFailure);
                }
                finally
                {
                    try
                    {
                        if (harness.Actual is not null)
                            await harness.Actual.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                    finally
                    {
                        try { await harness.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
                        finally { LogContext.Current = previous; }
                    }
                }
            }
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    private static async Task JoinExpectedAsync(Task task, params Exception[] expected)
    {
        try { await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
        catch (Exception exception) when (expected.Any(cause => ReferenceEquals(cause, exception))) { }
    }

    private static IEnumerable<Exception> ExceptionTree(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.InnerExceptions)
                foreach (Exception cause in ExceptionTree(inner))
                    yield return cause;
        }
        else if (exception.InnerException is Exception inner)
            foreach (Exception cause in ExceptionTree(inner))
                yield return cause;
    }

    private sealed class ProbeHarness(TimeProvider clock) : AsyncTestHarness(clock) { }

    private sealed class FailingHarness(IBusControl bus, Exception failure) : BusTestHarness
    {
        public override string InputQueueName => "unique-failed-start";
        public override Uri InputQueueAddress => new("loopback://localhost/unique-failed-start");
        protected override Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken) => Task.FromResult(bus);
        protected override void ConnectObservers(IBus createdBus) => throw failure;
    }

    private sealed class ForwardingHarness : InMemoryTestHarness
    {
        public IBusControl? Actual;
        public BusProxy? Proxy;
        protected override async Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken)
        {
            Actual = await base.CreateBusAsync(cancellationToken);
            IBusControl bus = DispatchProxy.Create<IBusControl, BusProxy>();
            Proxy = (BusProxy)(object)bus;
            Proxy.Actual = Actual;
            return bus;
        }
    }

    public class BusProxy : DispatchProxy
    {
        public IBusControl? Actual;
        public Exception? StopFailure;
        public int StopCalls;
        public CancellationToken StopToken;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusControl.StopAsync))
            {
                StopCalls++;
                StopToken = (CancellationToken)args![0]!;
                if (StopFailure is not null)
                    return Task.FromException(StopFailure);
                if (Actual is null)
                    return Task.CompletedTask;
            }
            if (Actual is null || targetMethod is null)
                throw new InvalidOperationException("Unexpected harness fixture bus invocation.");
            try { return targetMethod.Invoke(Actual, args); }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class ErrorLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, Exception? Exception);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount;
        public Exception? ThrownFailure;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            string? template = values.FirstOrDefault(value => value.Key == "{OriginalFormat}").Value as string;
            Entries.Add(new Entry(logLevel, template, exception));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }
}
