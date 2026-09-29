using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class FilterObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "typed-and-untyped-success-order")]
    public async Task Observers_ReceiveTheSameContextInExactSuccessOrderAsync()
    {
        var trace = new List<string>();
        var router = new PipeRouter();
        CommandContext<SetConcurrencyLimit>? bodyContext = null;
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(context =>
        {
            bodyContext = context;
            trace.Add("body");
        }));
        var typed = new TypedObserver(trace);
        var untyped = new UntypedObserver(trace);
        var observerConnector = (IFilterObserverConnector)router;
        observerConnector.ConnectObserver(typed);
        observerConnector.ConnectObserver(untyped);

        await router.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["typed-pre", "untyped-pre", "body", "typed-post", "untyped-post"], trace);
        Assert.Same(bodyContext, typed.Context);
        Assert.Same(bodyContext, untyped.Context);
        Assert.Null(typed.Failure);
        Assert.Null(untyped.Failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "typed-and-untyped-fault-order")]
    public async Task Observers_ReceiveTheExactFailureAndNeverReportPostSendAsync()
    {
        var trace = new List<string>();
        var expected = new DispatchException("dispatch failed");
        var router = new PipeRouter();
        CommandContext<SetConcurrencyLimit>? bodyContext = null;
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(context =>
        {
            bodyContext = context;
            trace.Add("body");
            throw expected;
        }));
        var typed = new TypedObserver(trace);
        var untyped = new UntypedObserver(trace);
        var observerConnector = (IFilterObserverConnector)router;
        observerConnector.ConnectObserver(typed);
        observerConnector.ConnectObserver(untyped);

        DispatchException actual = await Assert.ThrowsAsync<DispatchException>(() => router.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(["typed-pre", "untyped-pre", "body", "typed-fault", "untyped-fault"], trace);
        Assert.Same(expected, actual);
        Assert.Same(expected, typed.Failure);
        Assert.Same(expected, untyped.Failure);
        Assert.Same(bodyContext, typed.Context);
        Assert.Same(bodyContext, untyped.Context);
    }

    [Theory]
    [InlineData("typed-pre", false)]
    [InlineData("typed-pre", true)]
    [InlineData("untyped-pre", false)]
    [InlineData("untyped-pre", true)]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "pre-send-failure-reports-fault-without-dispatch")]
    public async Task FailingPreSendObserver_ReportsTheOriginalFaultWithoutDispatchAsync(string failingStage, bool synchronous)
    {
        var trace = new List<string>();
        var expected = new DispatchException("pre-send observer failed");
        Task FailAtStageAsync(string stage)
        {
            if (stage != failingStage)
                return Task.CompletedTask;

            if (synchronous)
                throw expected;

            return Task.FromException(expected);
        }
        var router = new PipeRouter();
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(_ => trace.Add("body")));
        var typed = new TypedObserver(trace, FailAtStageAsync);
        var untyped = new UntypedObserver(trace, FailAtStageAsync);
        var connector = (IFilterObserverConnector)router;
        using ConnectHandle typedHandle = connector.ConnectObserver(typed);
        using ConnectHandle untypedHandle = connector.ConnectObserver(untyped);

        DispatchException actual = await Assert.ThrowsAsync<DispatchException>(() =>
            router.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(failingStage == "typed-pre"
            ? ["typed-pre", "typed-fault", "untyped-fault"]
            : ["typed-pre", "untyped-pre", "typed-fault", "untyped-fault"], trace);
        Assert.Same(expected, typed.Failure);
        Assert.Same(expected, untyped.Failure);
        Assert.Same(typed.Context, untyped.Context);
    }

    [Theory]
    [InlineData("typed-pre")]
    [InlineData("untyped-pre")]
    [InlineData("typed-post")]
    [InlineData("untyped-post")]
    [InlineData("typed-fault")]
    [InlineData("untyped-fault")]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "asynchronous-observers-preserve-order-and-original-fault")]
    public async Task DelayedObserver_HoldsThePipelineAtItsStageAndPreservesTheOriginalOutcomeAsync(string delayedStage)
    {
        var trace = new List<string>();
        var router = new PipeRouter();
        var expectedFailure = new DispatchException("dispatch failed after asynchronous observation");
        bool faults = delayedStage.EndsWith("fault", StringComparison.Ordinal);
        string[] expectedTrace = faults
            ? ["typed-pre", "untyped-pre", "body", "typed-fault", "untyped-fault"]
            : ["typed-pre", "untyped-pre", "body", "typed-post", "untyped-post"];
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task WaitAtStageAsync(string stage)
        {
            if (stage != delayedStage)
                return Task.CompletedTask;
            entered.TrySetResult();
            return gate.Task;
        }
        CommandContext<SetConcurrencyLimit>? bodyContext = null;
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(context =>
        {
            bodyContext = context;
            trace.Add("body");
            if (faults)
                throw expectedFailure;
        }));
        var typed = new TypedObserver(trace, WaitAtStageAsync);
        var untyped = new UntypedObserver(trace, WaitAtStageAsync);
        var observerConnector = (IFilterObserverConnector)router;
        using ConnectHandle typedHandle = observerConnector.ConnectObserver(typed);
        using ConnectHandle untypedHandle = observerConnector.ConnectObserver(untyped);

        Task operation = router.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            int blockedIndex = Array.IndexOf(expectedTrace, delayedStage);
            Assert.True(blockedIndex >= 0);
            Assert.Equal(expectedTrace.Take(blockedIndex + 1), trace);
            Assert.False(operation.IsCompleted);
            if (blockedIndex < 2)
                Assert.Null(bodyContext);

            gate.SetResult();
            if (faults)
                Assert.Same(expectedFailure, await Assert.ThrowsAsync<DispatchException>(() => operation));
            else
                await operation;
        }
        finally
        {
            gate.TrySetResult();
            _ = await Record.ExceptionAsync(() => operation);
        }

        Assert.Equal(expectedTrace, trace);
        Assert.NotNull(bodyContext);
        Assert.Same(bodyContext, typed.Context);
        Assert.Same(bodyContext, untyped.Context);
        Assert.Same(faults ? expectedFailure : null, typed.Failure);
        Assert.Same(faults ? expectedFailure : null, untyped.Failure);
    }

    [Theory]
    [InlineData("typed-fault", false)]
    [InlineData("typed-fault", true)]
    [InlineData("untyped-fault", false)]
    [InlineData("untyped-fault", true)]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "fault-observer-failure-does-not-replace-dispatch-failure")]
    public async Task FailingFaultObserver_DoesNotReplaceTheDispatchFailureOrSkipTheOtherObserverAsync(string failingStage, bool loggerThrows)
    {
        var trace = new List<string>();
        var router = new PipeRouter();
        var dispatchFailure = new DispatchException("dispatch failed");
        var observerFailure = new InvalidOperationException("fault observer failed");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task FailAtStageAsync(string stage)
        {
            if (stage != failingStage)
                return Task.CompletedTask;
            entered.TrySetResult();
            return gate.Task;
        }
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(_ =>
        {
            trace.Add("body");
            throw dispatchFailure;
        }));
        var typed = new TypedObserver(trace, FailAtStageAsync);
        var untyped = new UntypedObserver(trace, FailAtStageAsync);
        var observerConnector = (IFilterObserverConnector)router;
        using ConnectHandle typedHandle = observerConnector.ConnectObserver(typed);
        using ConnectHandle untypedHandle = observerConnector.ConnectObserver(untyped);
        var logger = new ThrowingLogger();
        var previousLogContext = LogContext.Current;
        if (loggerThrows)
            LogContext.ConfigureCurrentLogContext(logger);

        try
        {
            Task operation = router.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken);
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.Equal(failingStage == "typed-fault"
                    ? ["typed-pre", "untyped-pre", "body", "typed-fault"]
                    : ["typed-pre", "untyped-pre", "body", "typed-fault", "untyped-fault"], trace);
                Assert.False(operation.IsCompleted);

                gate.SetException(observerFailure);
                Assert.Same(dispatchFailure, await Assert.ThrowsAsync<DispatchException>(() => operation));
            }
            finally
            {
                gate.TrySetException(observerFailure);
                _ = await Record.ExceptionAsync(() => operation);
            }

            Assert.Equal(["typed-pre", "untyped-pre", "body", "typed-fault", "untyped-fault"], trace);
            Assert.Same(dispatchFailure, typed.Failure);
            Assert.Same(dispatchFailure, untyped.Failure);
            if (loggerThrows)
            {
                Assert.Equal(1, logger.CallCount);
                Assert.Same(observerFailure, logger.ObservedFailure);
            }
        }
        finally
        {
            if (loggerThrows)
                LogContext.Current = previousLogContext;
        }
    }

    private sealed class TypedObserver(List<string> trace, Func<string, Task>? waitAtStage = null) : IFilterObserver<CommandContext<SetConcurrencyLimit>>
    {
        public CommandContext<SetConcurrencyLimit>? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreSendAsync(CommandContext<SetConcurrencyLimit> context)
        {
            Context = context;
            trace.Add("typed-pre");
            return waitAtStage?.Invoke("typed-pre") ?? Task.CompletedTask;
        }

        public Task PostSendAsync(CommandContext<SetConcurrencyLimit> context)
        {
            Assert.Same(Context, context);
            trace.Add("typed-post");
            return waitAtStage?.Invoke("typed-post") ?? Task.CompletedTask;
        }

        public Task SendFaultAsync(CommandContext<SetConcurrencyLimit> context, Exception exception)
        {
            Assert.Same(Context, context);
            Failure = exception;
            trace.Add("typed-fault");
            return waitAtStage?.Invoke("typed-fault") ?? Task.CompletedTask;
        }
    }

    private sealed class UntypedObserver(List<string> trace, Func<string, Task>? waitAtStage = null) : IFilterObserver
    {
        public CommandContext? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreSendAsync<T>(T context)
            where T : class, PipeContext
        {
            Context = Assert.IsAssignableFrom<CommandContext>(context);
            trace.Add("untyped-pre");
            return waitAtStage?.Invoke("untyped-pre") ?? Task.CompletedTask;
        }

        public Task PostSendAsync<T>(T context)
            where T : class, PipeContext
        {
            Assert.Same(Context, context);
            trace.Add("untyped-post");
            return waitAtStage?.Invoke("untyped-post") ?? Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(T context, Exception exception)
            where T : class, PipeContext
        {
            if (Context == null)
                Context = Assert.IsAssignableFrom<CommandContext>(context);
            else
                Assert.Same(Context, context);
            Failure = exception;
            trace.Add("untyped-fault");
            return waitAtStage?.Invoke("untyped-fault") ?? Task.CompletedTask;
        }
    }

    private sealed class DispatchException(string message) : Exception(message);

    private sealed class ThrowingLogger : ILogger
    {
        public int CallCount { get; private set; }

        public Exception? ObservedFailure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CallCount++;
            ObservedFailure = exception;
            throw new InvalidOperationException("diagnostic logger failed");
        }
    }
}
