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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "converted-continuation-task-is-owned-through-held-success-fault-and-cancellation")]
    public async Task ConvertedContinuation_RemainsOwnedThroughCompletionFaultAndCancellationAsync(int outcome)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        TimeSpan timeout = TimeSpan.FromSeconds(5);
        var trace = new List<string>();
        (ConsumeContext source, MessageConsumeContext<TestMessage> typed) = CreateContexts(new TestMessage("continuation-owned"));
        var primary = new DispatchException("unique-real-converted-continuation-failure");
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var continuationCancellation = new CancellationTokenSource();
        continuationCancellation.Cancel();
        Task rawNext = outcome switch
        {
            0 => Task.CompletedTask,
            1 => held.Task,
            2 => Task.FromException(primary),
            3 => Task.FromCanceled(continuationCancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        var outer = new ConsumeObservable();
        var filter = new ConsumeContextOutputMessageTypeFilter<TestMessage>(outer, new RequestIdTeeFilter<TestMessage>());
        var typedObserver = new TypedObserver(trace, _ => Task.CompletedTask);
        var outerObserver = new OuterObserver(trace, _ => Task.CompletedTask);
        ConsumeContext<TestMessage>? bodyContext = null;
        ConsumeContext? nextContext = null;
        var bodyCalls = 0;
        var nextCalls = 0;
        using ConnectHandle bodyHandle = filter.ConnectPipe(Pipe.ExecuteAwaited<ConsumeContext<TestMessage>>(context =>
        {
            bodyContext = context;
            bodyCalls++;
            trace.Add("body");
            return Task.CompletedTask;
        }));
        using ConnectHandle typedHandle = filter.ConnectConsumeMessageObserver(typedObserver);
        using ConnectHandle outerHandle = outer.Connect(outerObserver);
        IPipe<ConsumeContext> next = Pipe.ExecuteAwaited<ConsumeContext>(context =>
        {
            nextContext = context;
            nextCalls++;
            trace.Add("next");
            return rawNext;
        });
        Task? operation = null;
        try
        {
            operation = filter.SendAsync(source, next);
            Assert.Equal(1, bodyCalls);
            Assert.Equal(1, nextCalls);
            Assert.Same(typed, bodyContext);
            Assert.Same(typed, nextContext);
            Assert.NotSame(source, nextContext);
            if (outcome == 1)
            {
                Assert.False(rawNext.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Equal(["typed-pre", "outer-pre", "body", "next"], trace);
                held.SetResult();
            }
            if (outcome == 3)
                Assert.True(operation.IsCanceled);

            Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(timeout, token));
            if (outcome == 2)
            {
                Assert.Same(primary, observed);
                Assert.True(operation.IsFaulted);
                Assert.Same(primary, typedObserver.Failure);
                Assert.Same(primary, outerObserver.Failure);
            }
            else if (outcome == 3)
            {
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(observed);
                Assert.Equal(continuationCancellation.Token, canceled.CancellationToken);
                OperationCanceledException observerFailure = Assert.IsAssignableFrom<OperationCanceledException>(typedObserver.Failure);
                Assert.Equal(continuationCancellation.Token, observerFailure.CancellationToken);
                Assert.Same(typedObserver.Failure, outerObserver.Failure);
            }
            else
            {
                Assert.Null(observed);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.True(rawNext.IsCompletedSuccessfully);
                Assert.Null(typedObserver.Failure);
                Assert.Null(outerObserver.Failure);
            }
            Assert.Equal(outcome >= 2
                ? ["typed-pre", "outer-pre", "body", "next", "typed-fault", "outer-fault"]
                : ["typed-pre", "outer-pre", "body", "next", "typed-post", "outer-post"], trace);
            Assert.Same(typed, typedObserver.Context);
            Assert.Same(typed, outerObserver.Context);
        }
        finally
        {
            held.TrySetResult();
            try
            {
                await rawNext.WaitAsync(timeout, CancellationToken.None);
            }
            catch (Exception failure) when (failure is not TimeoutException && (rawNext.IsFaulted || rawNext.IsCanceled))
            {
                // Always observe the actual continuation task, including the task detached by the original adapter.
            }
            if (operation is not null)
            {
                try
                {
                    await operation.WaitAsync(timeout, CancellationToken.None);
                }
                catch (Exception failure) when (failure is not TimeoutException && (operation.IsFaulted || operation.IsCanceled))
                {
                    // Preserve finite assertion failures while observing the terminal original/fixed operation.
                }
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "parent-fanout-later-conversion-or-pipe-failure-joins-started-output")]
    public async Task ParentFanout_LaterConversionOrPipeFailureKeepsStartedWorkOwnedAsync(bool conversionThrows)
    {
        var firstMessage = new TestMessage("first-owned-fanout");
        var secondMessage = new SecondFanoutMessage("second-fanout-failure");
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, FanoutSourceConsumeContextProxy>();
        var firstContext = new MessageConsumeContext<TestMessage>(source, firstMessage);
        var secondContext = new MessageConsumeContext<SecondFanoutMessage>(source, secondMessage);
        var proxy = (FanoutSourceConsumeContextProxy)(object)source;
        var primary = new IOException("unique-parent-fanout-conversion-or-pipe-failure");
        proxy.Configure(firstContext, secondContext, conversionThrows ? primary : null);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCalls = 0;
        var secondCalls = 0;
        var nextCalls = 0;
        ConsumeContext<TestMessage>? firstReceived = null;
        ConsumeContext<SecondFanoutMessage>? secondReceived = null;
        Task? secondTask = null;
        var filter = new ConsumeContextMessageTypeFilter();
        using ConnectHandle firstHandle = filter.ConnectMessagePipe(Pipe.ExecuteAwaited<ConsumeContext<TestMessage>>(actual =>
        {
            firstCalls++;
            firstReceived = actual;
            return gate.Task;
        }));
        using ConnectHandle secondHandle = filter.ConnectMessagePipe(Pipe.ExecuteAwaited<ConsumeContext<SecondFanoutMessage>>(actual =>
        {
            secondCalls++;
            secondReceived = actual;
            secondTask = Task.FromException(primary);
            return secondTask;
        }));
        IPipe<ConsumeContext> next = Pipe.ExecuteAwaited<ConsumeContext>(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        Task? operation = null;
        try
        {
            operation = filter.SendAsync(source, next);
            Assert.Equal(new[] { typeof(TestMessage), typeof(SecondFanoutMessage) }, proxy.ConversionTypes);
            Assert.Equal(1, firstCalls);
            Assert.Same(firstContext, firstReceived);
            Assert.Same(firstMessage, firstReceived!.Message);
            Assert.Equal(conversionThrows ? 0 : 1, secondCalls);
            if (!conversionThrows)
            {
                Assert.Same(secondContext, secondReceived);
                Assert.Same(secondMessage, secondReceived!.Message);
                Assert.NotNull(secondTask);
                Assert.True(secondTask.IsFaulted);
            }
            Assert.Equal(0, nextCalls);
            Assert.False(gate.Task.IsCompleted);
            Assert.False(operation.IsCompleted);
            gate.SetResult();
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Same(primary, observed);
            Assert.True(operation.IsFaulted);
            Assert.True(gate.Task.IsCompletedSuccessfully);
            Assert.Equal(0, nextCalls);
            Assert.Same(primary, Assert.Single(operation.Exception!.Flatten().InnerExceptions));
        }
        finally
        {
            gate.TrySetResult();
            await JoinParentFanoutTaskAsync(gate.Task);
            if (secondTask is not null)
                await JoinParentFanoutTaskAsync(secondTask);
            if (operation is not null)
                await JoinParentFanoutTaskAsync(operation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "parent-fanout-admission-preserves-all-started-faults-or-cancellation")]
    public async Task ParentFanout_AdmissionFailureJoinsAllStartedOutcomesAsync(bool firstCanceled)
    {
        using var caller = new CancellationTokenSource();
        var admission = new IOException("unique-third-output-conversion-failure");
        var firstFailure = new IOException("unique-first-started-output-failure");
        var secondFailure = new ApplicationException("unique-second-started-output-failure");
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, MultiOwnedFanoutSourceProxy>();
        var proxy = (MultiOwnedFanoutSourceProxy)(object)source;
        var firstContext = new MessageConsumeContext<TestMessage>(source, new TestMessage("first-owned"));
        var secondContext = new MessageConsumeContext<SecondFanoutMessage>(source, new SecondFanoutMessage("second-owned"));
        proxy.Configure(firstContext, secondContext, admission, caller.Token);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsumeContext<TestMessage>? firstReceived = null;
        ConsumeContext<SecondFanoutMessage>? secondReceived = null;
        var firstCalls = 0;
        var secondCalls = 0;
        var thirdCalls = 0;
        var nextCalls = 0;
        var filter = new ConsumeContextMessageTypeFilter();
        using ConnectHandle firstHandle = filter.ConnectMessagePipe(Pipe.ExecuteAwaited<ConsumeContext<TestMessage>>(context =>
        {
            firstCalls++;
            firstReceived = context;
            return firstGate.Task;
        }));
        using ConnectHandle secondHandle = filter.ConnectMessagePipe(Pipe.ExecuteAwaited<ConsumeContext<SecondFanoutMessage>>(context =>
        {
            secondCalls++;
            secondReceived = context;
            return secondGate.Task;
        }));
        using ConnectHandle thirdHandle = filter.ConnectMessagePipe(Pipe.ExecuteAwaited<ConsumeContext<ThirdFanoutMessage>>(_ =>
        {
            thirdCalls++;
            return Task.CompletedTask;
        }));
        IPipe<ConsumeContext> next = Pipe.ExecuteAwaited<ConsumeContext>(_ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });
        Task? operation = null;
        try
        {
            operation = filter.SendAsync(source, next);
            Assert.Equal(new[] { typeof(TestMessage), typeof(SecondFanoutMessage), typeof(ThirdFanoutMessage) }, proxy.ConversionTypes);
            Assert.Equal(1, firstCalls);
            Assert.Equal(1, secondCalls);
            Assert.Equal(0, thirdCalls);
            Assert.Equal(0, nextCalls);
            Assert.Same(firstContext, firstReceived);
            Assert.Same(secondContext, secondReceived);
            Assert.Equal(caller.Token, firstReceived!.CancellationToken);
            Assert.Equal(caller.Token, secondReceived!.CancellationToken);
            Assert.False(firstGate.Task.IsCompleted);
            Assert.False(secondGate.Task.IsCompleted);
            Assert.False(operation.IsCompleted);

            if (firstCanceled)
            {
                caller.Cancel();
                firstGate.SetCanceled(caller.Token);
                secondGate.SetResult();
            }
            else
            {
                firstGate.SetException(firstFailure);
                secondGate.SetException(secondFailure);
            }
            Exception? observed = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.True(firstGate.Task.IsCompleted);
            Assert.True(secondGate.Task.IsCompleted);
            Assert.True(operation.IsFaulted);
            Assert.Equal(0, nextCalls);
            Assert.Equal(0, thirdCalls);
            AggregateException aggregate = Assert.IsType<AggregateException>(observed);
            Assert.Same(admission, aggregate.InnerExceptions[0]);
            var causes = aggregate.Flatten().InnerExceptions;
            Assert.Same(admission, Assert.Single(causes, failure => ReferenceEquals(failure, admission)));
            if (firstCanceled)
            {
                Assert.Equal(2, causes.Count);
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(
                    Assert.Single(causes, failure => failure is OperationCanceledException));
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(firstGate.Task.IsCanceled);
                Assert.True(secondGate.Task.IsCompletedSuccessfully);
            }
            else
            {
                Assert.Equal(3, causes.Count);
                Assert.Same(firstFailure, Assert.Single(causes, failure => ReferenceEquals(failure, firstFailure)));
                Assert.Same(secondFailure, Assert.Single(causes, failure => ReferenceEquals(failure, secondFailure)));
                Assert.True(firstGate.Task.IsFaulted);
                Assert.True(secondGate.Task.IsFaulted);
            }
        }
        finally
        {
            firstGate.TrySetResult();
            secondGate.TrySetResult();
            try
            {
                await JoinParentFanoutTaskAsync(firstGate.Task);
            }
            finally
            {
                try
                {
                    await JoinParentFanoutTaskAsync(secondGate.Task);
                }
                finally
                {
                    if (operation is not null)
                        await JoinParentFanoutTaskAsync(operation);
                }
            }
        }
    }

    private class MultiOwnedFanoutSourceProxy : DispatchProxy
    {
        private ConsumeContext<TestMessage> _first = null!;
        private ConsumeContext<SecondFanoutMessage> _second = null!;
        private Exception _admission = null!;
        private CancellationToken _caller;
        public List<Type> ConversionTypes { get; } = [];

        public void Configure(ConsumeContext<TestMessage> first, ConsumeContext<SecondFanoutMessage> second,
            Exception admission, CancellationToken caller)
        {
            _first = first;
            _second = second;
            _admission = admission;
            _caller = caller;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_CancellationToken")
                return _caller;
            if (targetMethod.Name == "TryGetMessage" && targetMethod.IsGenericMethod)
            {
                Type type = targetMethod.GetGenericArguments()[0];
                ConversionTypes.Add(type);
                if (type == typeof(TestMessage))
                {
                    args![0] = _first;
                    return true;
                }
                if (type == typeof(SecondFanoutMessage))
                {
                    args![0] = _second;
                    return true;
                }
                if (type == typeof(ThirdFanoutMessage))
                    throw _admission;
                args![0] = null;
                return false;
            }
            throw new NotSupportedException($"Unexpected owned fanout context member: {targetMethod.Name}");
        }
    }

    private sealed record ThirdFanoutMessage(string Value);

    private static async Task JoinParentFanoutTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
            // Observe every actual started task even when the finite ownership assertion fails.
        }
    }

    private class FanoutSourceConsumeContextProxy : DispatchProxy
    {
        private ConsumeContext<TestMessage> _first = null!;
        private ConsumeContext<SecondFanoutMessage> _second = null!;
        private Exception? _conversionFailure;
        public List<Type> ConversionTypes { get; } = [];

        public void Configure(ConsumeContext<TestMessage> first, ConsumeContext<SecondFanoutMessage> second, Exception? conversionFailure)
        {
            _first = first;
            _second = second;
            _conversionFailure = conversionFailure;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "TryGetMessage" && targetMethod.IsGenericMethod)
            {
                Type messageType = targetMethod.GetGenericArguments()[0];
                ConversionTypes.Add(messageType);
                if (messageType == typeof(TestMessage))
                {
                    args![0] = _first;
                    return true;
                }
                if (messageType == typeof(SecondFanoutMessage))
                {
                    if (_conversionFailure is not null)
                        throw _conversionFailure;
                    args![0] = _second;
                    return true;
                }
                args![0] = null;
                return false;
            }
            throw new NotSupportedException($"Unexpected fanout consume-context member: {targetMethod.Name}");
        }
    }

    private sealed record SecondFanoutMessage(string Value);

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
