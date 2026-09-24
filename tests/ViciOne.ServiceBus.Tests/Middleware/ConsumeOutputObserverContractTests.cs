using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ConsumeOutputObserverContractTests
{
    [Theory]
    [InlineData("typed-pre")]
    [InlineData("outer-pre")]
    [InlineData("typed-post")]
    [InlineData("outer-post")]
    [InlineData("typed-fault")]
    [InlineData("outer-fault")]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "asynchronous-output-observers-preserve-order-and-dispatch-fault")]
    public async Task DelayedObserver_BlocksFollowingStagesAndPreservesTheDispatchOutcomeAsync(string delayedStage)
    {
        var trace = new List<string>();
        var message = new TestMessage("observed");
        (ConsumeContext sourceContext, MessageConsumeContext<TestMessage> typedContext) = CreateContexts(message);
        Assert.NotSame(sourceContext, typedContext);
        var dispatchFailure = new DispatchException("consume dispatch failed");
        bool faults = delayedStage.EndsWith("fault", StringComparison.Ordinal);
        string[] expectedTrace = faults
            ? ["typed-pre", "outer-pre", "body", "typed-fault", "outer-fault"]
            : ["typed-pre", "outer-pre", "body", "next", "typed-post", "outer-post"];
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task WaitAtStageAsync(string stage)
        {
            if (stage != delayedStage)
                return Task.CompletedTask;
            entered.TrySetResult();
            return gate.Task;
        }
        var outer = new ConsumeObservable();
        var filter = new ConsumeContextOutputMessageTypeFilter<TestMessage>(outer, new RequestIdTeeFilter<TestMessage>());
        using ConnectHandle bodyHandle = filter.ConnectPipe(Pipe.Execute<ConsumeContext<TestMessage>>(received =>
        {
            Assert.Same(typedContext, received);
            Assert.NotSame(sourceContext, received);
            trace.Add("body");
            if (faults)
                throw dispatchFailure;
        }));
        var typedObserver = new TypedObserver(trace, WaitAtStageAsync);
        var outerObserver = new OuterObserver(trace, WaitAtStageAsync);
        using ConnectHandle typedHandle = filter.ConnectConsumeMessageObserver(typedObserver);
        using ConnectHandle outerHandle = outer.Connect(outerObserver);
        IPipe<ConsumeContext> next = Pipe.Execute<ConsumeContext>(received =>
        {
            Assert.Same(typedContext, received);
            Assert.NotSame(sourceContext, received);
            trace.Add("next");
        });

        Task operation = filter.SendAsync(sourceContext, next);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            int blockedIndex = Array.IndexOf(expectedTrace, delayedStage);
            Assert.True(blockedIndex >= 0);
            Assert.Equal(expectedTrace.Take(blockedIndex + 1), trace);
            Assert.False(operation.IsCompleted);

            gate.SetResult();
            if (faults)
                Assert.Same(dispatchFailure, await Assert.ThrowsAsync<DispatchException>(() => operation));
            else
                await operation;
        }
        finally
        {
            gate.TrySetResult();
            _ = await Record.ExceptionAsync(() => operation);
        }

        Assert.Equal(expectedTrace, trace);
        Assert.Same(typedContext, typedObserver.Context);
        Assert.Same(typedContext, outerObserver.Context);
        Assert.NotSame(sourceContext, typedObserver.Context);
        Assert.NotSame(sourceContext, outerObserver.Context);
        Assert.Same(faults ? dispatchFailure : null, typedObserver.Failure);
        Assert.Same(faults ? dispatchFailure : null, outerObserver.Failure);
        Assert.Same(message, typedContext.Message);
    }

    [Theory]
    [InlineData("typed-fault", false)]
    [InlineData("typed-fault", true)]
    [InlineData("outer-fault", false)]
    [InlineData("outer-fault", true)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "output-fault-observer-and-logger-failure-preserve-original")]
    public async Task FailingFaultObserver_CannotReplaceTheDispatchFailureOrSkipTheOuterObserverAsync(string failingStage, bool loggerThrows)
    {
        var trace = new List<string>();
        (ConsumeContext sourceContext, MessageConsumeContext<TestMessage> typedContext) = CreateContexts(new TestMessage("failing"));
        Assert.NotSame(sourceContext, typedContext);
        var dispatchFailure = new DispatchException("consume dispatch failed");
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
        var outer = new ConsumeObservable();
        var filter = new ConsumeContextOutputMessageTypeFilter<TestMessage>(outer, new RequestIdTeeFilter<TestMessage>());
        using ConnectHandle bodyHandle = filter.ConnectPipe(Pipe.Execute<ConsumeContext<TestMessage>>(received =>
        {
            Assert.Same(typedContext, received);
            Assert.NotSame(sourceContext, received);
            trace.Add("body");
            throw dispatchFailure;
        }));
        var typedObserver = new TypedObserver(trace, FailAtStageAsync);
        var outerObserver = new OuterObserver(trace, FailAtStageAsync);
        using ConnectHandle typedHandle = filter.ConnectConsumeMessageObserver(typedObserver);
        using ConnectHandle outerHandle = outer.Connect(outerObserver);
        var logger = new ThrowingLogger();
        var previousLogContext = LogContext.Current;
        if (loggerThrows)
            LogContext.ConfigureCurrentLogContext(logger);

        try
        {
            Task operation = filter.SendAsync(sourceContext, Pipe.Execute<ConsumeContext>(_ => trace.Add("next")));
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.Equal(failingStage == "typed-fault"
                    ? ["typed-pre", "outer-pre", "body", "typed-fault"]
                    : ["typed-pre", "outer-pre", "body", "typed-fault", "outer-fault"], trace);
                Assert.False(operation.IsCompleted);

                gate.SetException(observerFailure);
                Assert.Same(dispatchFailure, await Assert.ThrowsAsync<DispatchException>(() => operation));
            }
            finally
            {
                gate.TrySetException(observerFailure);
                _ = await Record.ExceptionAsync(() => operation);
            }

            Assert.Equal(["typed-pre", "outer-pre", "body", "typed-fault", "outer-fault"], trace);
            Assert.Same(typedContext, typedObserver.Context);
            Assert.Same(typedContext, outerObserver.Context);
            Assert.NotSame(sourceContext, typedObserver.Context);
            Assert.NotSame(sourceContext, outerObserver.Context);
            Assert.Same(dispatchFailure, typedObserver.Failure);
            Assert.Same(dispatchFailure, outerObserver.Failure);
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

    private static (ConsumeContext Source, MessageConsumeContext<TestMessage> Typed) CreateContexts(TestMessage message)
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, SourceConsumeContextProxy>();
        var typed = new MessageConsumeContext<TestMessage>(source, message);
        ((SourceConsumeContextProxy)(object)source).Configure(typed);
        return (source, typed);
    }

    private class SourceConsumeContextProxy : DispatchProxy
    {
        private MessageConsumeContext<TestMessage> _typed = null!;

        public void Configure(MessageConsumeContext<TestMessage> typed)
        {
            _typed = typed;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "TryGetMessage" && targetMethod.IsGenericMethod)
            {
                bool matches = targetMethod.GetGenericArguments()[0] == typeof(TestMessage);
                args![0] = matches ? _typed : null;
                return matches;
            }
            throw new NotSupportedException($"Unexpected consume-context member: {targetMethod.Name}");
        }
    }

    private sealed class TypedObserver(List<string> trace, Func<string, Task> waitAtStage) : IConsumeMessageObserver<TestMessage>
    {
        public ConsumeContext<TestMessage>? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreConsumeAsync(ConsumeContext<TestMessage> context)
        {
            Context = context;
            trace.Add("typed-pre");
            return waitAtStage("typed-pre");
        }

        public Task PostConsumeAsync(ConsumeContext<TestMessage> context)
        {
            Assert.Same(Context, context);
            trace.Add("typed-post");
            return waitAtStage("typed-post");
        }

        public Task ConsumeFaultAsync(ConsumeContext<TestMessage> context, Exception exception)
        {
            Assert.Same(Context, context);
            Failure = exception;
            trace.Add("typed-fault");
            return waitAtStage("typed-fault");
        }
    }

    private sealed class OuterObserver(List<string> trace, Func<string, Task> waitAtStage) : IConsumeObserver
    {
        public ConsumeContext<TestMessage>? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            Context = Assert.IsAssignableFrom<ConsumeContext<TestMessage>>(context);
            trace.Add("outer-pre");
            return waitAtStage("outer-pre");
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            Assert.Same(Context, context);
            trace.Add("outer-post");
            return waitAtStage("outer-post");
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            Assert.Same(Context, context);
            Failure = exception;
            trace.Add("outer-fault");
            return waitAtStage("outer-fault");
        }
    }

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

    private sealed record TestMessage(string Value);

    private sealed class DispatchException(string message) : Exception(message);
}
