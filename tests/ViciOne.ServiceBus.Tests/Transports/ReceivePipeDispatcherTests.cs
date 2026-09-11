using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceivePipeDispatcherTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCH", "primary-failure-remains-authoritative")]
    public async Task DispatchAsync_ObserverAndZeroActivityFailuresDoNotReplaceThePipelineFailureAsync()
    {
        var pipelineFailure = new ExpectedPipelineException();
        var observerFailure = new ExpectedObserverException();
        var receiveLock = new RecordingReceiveLock();
        var dispatcher = CreateDispatcher(_ => Task.FromException(pipelineFailure));
        dispatcher.ConnectReceiveObserver(new FaultingReceiveObserver(observerFailure));
        dispatcher.ZeroActivity += () => Task.FromException(new ExpectedZeroActivityException());

        Exception actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
            DispatchWithoutAmbientLoggingAsync(dispatcher, new TestReceiveContext(), receiveLock));

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

    private static ReceivePipeDispatcher CreateDispatcher(Func<ReceiveContext, Task> dispatch)
    {
        var hostConfiguration = DispatchProxy.Create<IHostConfiguration, HostConfigurationProxy>();
        return new ReceivePipeDispatcher(
            new CallbackReceivePipe(dispatch),
            new ReceiveObservable(),
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

    private sealed class TestReceiveContext : BasePipeContext, ReceiveContext
    {
        public TimeSpan ElapsedTime => TimeSpan.Zero;
        public Uri InputAddress { get; } = new("loopback://localhost/orders");
        public ContentType ContentType { get; } = new("application/json");
        public bool Redelivered => false;
        public Headers TransportHeaders { get; } = new JsonTransportHeaders(new DictionaryHeaderProvider());
        public Task ReceiveCompleted => Task.CompletedTask;
        public bool IsDelivered => false;
        public bool IsFaulted => false;
        public ISendEndpointProvider SendEndpointProvider => throw new NotSupportedException();
        public IPublishEndpointProvider PublishEndpointProvider => throw new NotSupportedException();
        public bool PublishFaults => false;
        public MessageBody Body { get; } = new BytesMessageBody([]);

        public Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            Exception exception, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

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

    private sealed class ExpectedPipelineException : Exception;
    private sealed class ExpectedObserverException : Exception;
    private sealed class ExpectedSettlementException : Exception;
    private sealed class ExpectedZeroActivityException : Exception;
}
