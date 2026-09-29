using System.Net.Mime;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Rescue;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceivePipeDispatcherTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "rethrow-notifies-once-and-records-fault-before-settlement")]
    public async Task RethrowFaultedMessage_ReportsOneOriginalFaultAndMarksDeliveryFaultedAsync()
    {
        var failure = new ExpectedPipelineException();
        var observers = new ReceiveObservable();
        var observer = new CountingReceiveObserver();
        observers.Connect(observer);
        var context = new TestReceiveContext(observers: observers);
        var receiveLock = new RecordingReceiveLock();
        var downstreamCalls = 0;
        var filter = new RethrowErrorTransportFilter();
        var dispatcher = CreateDispatcher(receive => filter.SendAsync(
            RescueContextTestFactory.Create(receive, failure),
            Pipe.Execute<ExceptionReceiveContext>(_ => downstreamCalls++)), observers);

        ExpectedPipelineException actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
            DispatchWithoutAmbientLoggingAsync(dispatcher, context, receiveLock));

        Assert.Same(failure, actual);
        Assert.Same(failure, receiveLock.Failure);
        Assert.Same(context, observer.Context);
        Assert.Same(failure, observer.Failure);
        Assert.Equal(1, observer.FaultCount);
        Assert.True(context.IsFaulted);
        Assert.Equal(0, downstreamCalls);
        Assert.Equal(0, receiveLock.CompletionCount);
        Assert.Equal(1, receiveLock.FaultCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "fault-observer-and-logger-failures-cannot-skip-settlement")]
    public async Task FaultObserverAndLoggerFailures_DoNotReplaceTheDispatchFailureOrSkipSettlementAsync()
    {
        var primary = new ExpectedPipelineException();
        var observerFailure = new ExpectedObserverException();
        var loggerFailure = new InvalidOperationException("diagnostic logger failed");
        var receiveObservers = new ReceiveObservable();
        var dispatcher = CreateDispatcher(_ => Task.FromException(primary), receiveObservers);
        dispatcher.ConnectReceiveObserver(new FaultingReceiveObserver(observerFailure));
        var context = new TestReceiveContext(observers: receiveObservers);
        var receiveLock = new RecordingReceiveLock();
        var logger = new ThrowingErrorLogger(loggerFailure);
        ILogContext? previous = LogContext.Current;
        LogContext.ConfigureCurrentLogContext(logger);

        try
        {
            ExpectedPipelineException actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
                dispatcher.DispatchAsync(context, receiveLock, TestContext.Current.CancellationToken));

            Assert.Same(primary, actual);
            Assert.Same(primary, receiveLock.Failure);
            Assert.True(context.IsFaulted);
            Assert.Equal(1, logger.CallCount);
            Assert.Same(observerFailure, logger.ObservedFailure);
            Assert.Equal(1, receiveLock.FaultCount);
            Assert.Equal(0, receiveLock.CompletionCount);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "pending-receive-work-owns-settlement-and-successor-order")]
    public async Task DispatchCompletion_OwnsSettlementAndFailureNotificationUntilReceiveWorkEndsAsync(bool receiveWorkFails)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        var events = new List<string>();
        var work = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new ExpectedPipelineException();
        var receiveLock = new OrderedReceiveLock(events);
        var observer = new OrderedReceiveObserver(events);
        var receiveObservers = new ReceiveObservable();
        var dispatcher = CreateDispatcher(_ =>
        {
            events.Add("dispatch");
            dispatched.TrySetResult();
            return Task.CompletedTask;
        }, receiveObservers);
        dispatcher.ConnectReceiveObserver(observer);
        dispatcher.ZeroActivity += () =>
        {
            events.Add("zero");
            return Task.CompletedTask;
        };

        Task dispatch = DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(work.Task, receiveObservers), receiveLock);
        try
        {
            await dispatched.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.False(dispatch.IsCompleted);
            Assert.Equal(new[] { "pre", "validate", "dispatch" }, events);
            Assert.Equal(1, dispatcher.ActiveDispatchCount);
            Assert.Equal(1, dispatcher.DispatchCount);
            Assert.Null(receiveLock.Failure);
            Assert.Null(observer.Failure);
        }
        finally
        {
            if (receiveWorkFails)
                work.TrySetException(failure);
            else
                work.TrySetResult();

            try
            {
                await dispatch.WaitAsync(timeout, CancellationToken.None);
            }
            catch (ExpectedPipelineException) when (receiveWorkFails)
            {
            }
        }

        if (receiveWorkFails)
        {
            ExpectedPipelineException actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
                dispatch.WaitAsync(timeout, TestContext.Current.CancellationToken));
            Assert.Same(failure, actual);
            Assert.Same(failure, observer.Failure);
            Assert.Same(failure, receiveLock.Failure);
            Assert.Equal(new[] { "pre", "validate", "dispatch", "fault-observer", "fault-lock", "zero" }, events);
        }
        else
        {
            await dispatch.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.Null(observer.Failure);
            Assert.Null(receiveLock.Failure);
            Assert.Equal(new[] { "pre", "validate", "dispatch", "complete", "post", "zero" }, events);
        }
        Assert.Equal(0, dispatcher.ActiveDispatchCount);

        events.Clear();
        var successorLock = new OrderedReceiveLock(events);
        await DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(observers: receiveObservers), successorLock)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "pre", "validate", "dispatch", "complete", "post", "zero" }, events);
        Assert.Null(successorLock.Failure);
        Assert.Equal(0, dispatcher.ActiveDispatchCount);
        Assert.Equal(2, dispatcher.DispatchCount);
        Assert.Equal(1, dispatcher.MaxConcurrentDispatchCount);
        Assert.Equal(2, dispatcher.GetMetrics().DeliveryCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "primary-failure-remains-authoritative")]
    public async Task DispatchAsync_ObserverAndZeroActivityFailuresDoNotReplaceThePipelineFailureAsync()
    {
        var pipelineFailure = new ExpectedPipelineException();
        var observerFailure = new ExpectedObserverException();
        var receiveLock = new RecordingReceiveLock();
        var receiveObservers = new ReceiveObservable();
        var dispatcher = CreateDispatcher(_ => Task.FromException(pipelineFailure), receiveObservers);
        dispatcher.ConnectReceiveObserver(new FaultingReceiveObserver(observerFailure));
        dispatcher.ZeroActivity += () => Task.FromException(new ExpectedZeroActivityException());

        Exception actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
            DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(observers: receiveObservers), receiveLock));

        Assert.Same(pipelineFailure, actual);
        Assert.Same(pipelineFailure, receiveLock.Failure);
        Assert.Equal(1, receiveLock.ValidationCount);
        Assert.Equal(0, receiveLock.CompletionCount);
        Assert.Equal(1, receiveLock.FaultCount);
        Assert.Equal(0, dispatcher.ActiveDispatchCount);
        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(1, dispatcher.MaxConcurrentDispatchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "settlement-failure-preserves-both-causes")]
    public async Task DispatchAsync_FaultSettlementFailureReportsBothThePipelineAndSettlementFailuresAsync()
    {
        var pipelineFailure = new ExpectedPipelineException();
        var settlementFailure = new ExpectedSettlementException();
        var receiveLock = new RecordingReceiveLock(settlementFailure);
        var dispatcher = CreateDispatcher(_ => Task.FromException(pipelineFailure));

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(), receiveLock));

        Assert.StartsWith("Receive-lock fault settlement failed.", actual.Message, StringComparison.Ordinal);
        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Same(pipelineFailure, failure),
            failure => Assert.Same(settlementFailure, failure));
        Assert.Same(pipelineFailure, receiveLock.Failure);
        Assert.Equal(0, dispatcher.ActiveDispatchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "zero-activity-subscriber-isolation")]
    public async Task DispatchAsync_InvokesEveryZeroActivitySubscriberWithoutAffectingSuccessfulSettlementAsync()
    {
        var secondSubscriberCount = 0;
        var receiveLock = new RecordingReceiveLock();
        var dispatcher = CreateDispatcher(_ => Task.CompletedTask);
        dispatcher.ZeroActivity += () => Task.FromException(new ExpectedZeroActivityException());
        dispatcher.ZeroActivity += () =>
        {
            Interlocked.Increment(ref secondSubscriberCount);
            return Task.CompletedTask;
        };

        await DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(), receiveLock);

        Assert.Equal(1, secondSubscriberCount);
        Assert.Equal(1, receiveLock.ValidationCount);
        Assert.Equal(1, receiveLock.CompletionCount);
        Assert.Equal(0, receiveLock.FaultCount);
        Assert.Equal(0, dispatcher.ActiveDispatchCount);
    }

    private static ReceivePipeDispatcher CreateDispatcher(Func<ReceiveContext, Task> dispatch, ReceiveObservable? observers = null)
    {
        var hostConfiguration = DispatchProxy.Create<IHostConfiguration, HostConfigurationProxy>();
        return new ReceivePipeDispatcher(
            new CallbackReceivePipe(dispatch),
            observers ?? new ReceiveObservable(),
            hostConfiguration,
            new Uri("loopback://localhost/orders"));
    }

    private static async Task DispatchWithoutAmbientLoggingAsync(
        ReceivePipeDispatcher dispatcher,
        ReceiveContext context,
        ReceiveLockContext receiveLock)
    {
        ILogContext? previous = LogContext.Current;
        LogContext.Current = null;
        try
        {
            await dispatcher.DispatchAsync(context, receiveLock, TestContext.Current.CancellationToken);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    private class HostConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name == $"get_{nameof(IHostConfiguration.ReceiveLogContext)}"
                ? null
                : throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class CallbackReceivePipe(Func<ReceiveContext, Task> dispatch) : IReceivePipe
    {
        private readonly Func<ReceiveContext, Task> _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));

        public Task Connected => Task.CompletedTask;

        public Task SendAsync(ReceiveContext context) => _dispatch(context);

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => new EmptyConnectHandle();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TestReceiveContext(Task? completion = null, ReceiveObservable? observers = null) : BasePipeContext, ReceiveContext
    {
        private bool _isFaulted;
        public TimeSpan ElapsedTime => TimeSpan.Zero;
        public Uri InputAddress { get; } = new("loopback://localhost/orders");
        public ContentType ContentType { get; } = new("application/json");
        public bool Redelivered => false;
        public Headers TransportHeaders { get; } = new JsonTransportHeaders(new DictionaryHeaderProvider());
        public Task ReceiveCompleted => completion ?? Task.CompletedTask;
        public bool IsDelivered => false;
        public bool IsFaulted => _isFaulted;
        public ISendEndpointProvider SendEndpointProvider => throw new NotSupportedException();
        public IPublishEndpointProvider PublishEndpointProvider => throw new NotSupportedException();
        public bool PublishFaults => false;
        public MessageBody Body { get; } = new BinaryMessageBody(ReadOnlyMemory<byte>.Empty);

        public Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            Exception exception, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _isFaulted = true;
            return observers?.ReceiveFaultAsync(this, exception) ?? Task.CompletedTask;
        }

        public void AddReceiveTask(Task task)
        {
        }
    }

    private sealed class RecordingReceiveLock(Exception? faultFailure = null) : ReceiveLockContext
    {
        public int CompletionCount { get; private set; }
        public Exception? Failure { get; private set; }
        public int FaultCount { get; private set; }
        public int ValidationCount { get; private set; }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CompletionCount++;
            return Task.CompletedTask;
        }

        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Failure = exception;
            FaultCount++;
            return faultFailure is null ? Task.CompletedTask : Task.FromException(faultFailure);
        }

        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidationCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingReceiveObserver(Exception failure) : IReceiveObserver
    {
        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) => Task.FromException(failure);
    }

    private sealed class CountingReceiveObserver : IReceiveObserver
    {
        public ReceiveContext? Context { get; private set; }
        public Exception? Failure { get; private set; }
        public int FaultCount { get; private set; }

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Context = context;
            Failure = exception;
            FaultCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingErrorLogger(Exception failure) : ILogger
    {
        public int CallCount { get; private set; }
        public Exception? ObservedFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CallCount++;
            ObservedFailure = exception;
            throw failure;
        }
    }

    private sealed class OrderedReceiveLock(List<string> events) : ReceiveLockContext
    {
        public Exception? Failure { get; private set; }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            events.Add("complete");
            return Task.CompletedTask;
        }

        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            Failure = exception;
            events.Add("fault-lock");
            return Task.CompletedTask;
        }

        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
        {
            events.Add("validate");
            return Task.CompletedTask;
        }
    }

    private sealed class OrderedReceiveObserver(List<string> events) : IReceiveObserver
    {
        public Exception? Failure { get; private set; }

        public Task PreReceiveAsync(ReceiveContext context)
        {
            events.Add("pre");
            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(ReceiveContext context)
        {
            events.Add("post");
            return Task.CompletedTask;
        }

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Failure = exception;
            events.Add("fault-observer");
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed class ExpectedPipelineException : Exception;
    private sealed class ExpectedObserverException : Exception;
    private sealed class ExpectedSettlementException : Exception;
    private sealed class ExpectedZeroActivityException : Exception;
}
