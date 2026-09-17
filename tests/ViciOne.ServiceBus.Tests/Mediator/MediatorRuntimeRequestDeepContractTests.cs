using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Mediator.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorRuntimeRequestDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "request-exception-without-inner-is-preserved")]
    public async Task SendRequestAsync_RequestExceptionWithoutInnerFailureIsPreservedAsync()
    {
        var expected = new RequestException("request failed without an inner exception");
        var request = new ResponseRequest(NewId.NextGuid());
        var handle = new FaultingRequestHandle(request, expected);
        var createInvocations = 0;
        IMediator mediator = CreateProxy<IMediator>((method, arguments) =>
        {
            Assert.Equal(nameof(IMediator.CreateRequest), method.Name);
            Assert.Same(request, arguments![0]);
            Interlocked.Increment(ref createInvocations);
            return handle;
        });

        RequestException failure = await Assert.ThrowsAsync<RequestException>(() =>
            mediator.SendRequestAsync(request, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(expected, failure);
        Assert.Equal(1, Volatile.Read(ref createInvocations));
        Assert.Equal(1, handle.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-PUBLISH", "empty-pipe-publication-forms")]
    public async Task PublishAsync_EmptyPipesDispatchEveryMessageAndInitializerFormAsync()
    {
        var consumed = new ConcurrentQueue<string>();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<PublishedMessage>(context =>
            {
                consumed.Enqueue(context.Message.Value);
                return Task.CompletedTask;
            });
        });
        IAdvancedPublishEndpoint publish = ((IPublishEndpoint)mediator).Advanced();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await publish.PublishAsync(
            new PublishedMessage { Value = "typed-message" },
            Pipe.Empty<PublishContext<PublishedMessage>>(),
            cancellationToken);
        await publish.PublishAsync(
            new PublishedMessage { Value = "untyped-message" },
            Pipe.Empty<PublishContext>(),
            cancellationToken);
        await publish.PublishAsync<PublishedMessage>(
            new { Value = "typed-values" },
            Pipe.Empty<PublishContext<PublishedMessage>>(),
            cancellationToken);
        await publish.PublishAsync<PublishedMessage>(
            new { Value = "untyped-values" },
            Pipe.Empty<PublishContext>(),
            cancellationToken);

        Assert.Equal(
            ["typed-message", "untyped-message", "typed-values", "untyped-values"],
            consumed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-BOUNDARIES", "runtime-and-initializer-pipe-null-arguments")]
    public async Task PublishAsync_RuntimeAndInitializerPipeOverloadsRejectExactNullArgumentsAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        IAdvancedPublishEndpoint publish = ((IPublishEndpoint)mediator).Advanced();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await AssertParameterAsync(
            "message",
            () => publish.PublishAsync((object)null!, Pipe.Empty<PublishContext>(), cancellationToken));
        await AssertParameterAsync(
            "publishPipe",
            () => publish.PublishAsync(
                (object)new PublishedMessage { Value = "valid" },
                (IPipe<PublishContext>)null!,
                cancellationToken));
        await AssertParameterAsync(
            "values",
            () => publish.PublishAsync<PublishedMessage>(
                (object)null!,
                Pipe.Empty<PublishContext<PublishedMessage>>(),
                cancellationToken));
        await AssertParameterAsync(
            "values",
            () => publish.PublishAsync<PublishedMessage>(
                (object)null!,
                Pipe.Empty<PublishContext>(),
                cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "one-way-null-task-fails-explicitly")]
    public async Task OneWayHandler_RejectsANullTaskExplicitlyAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => new NullTaskOneWayHandler());
        });

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new OneWayRequest("null-task"), TestContext.Current.CancellationToken));

        Assert.Equal(
            $"The mediator request handler '{typeof(NullTaskOneWayHandler).FullName}' returned a null task.",
            failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-HANDLER", "response-null-task-fails-explicitly")]
    public async Task ResponseHandler_RejectsANullTaskExplicitlyAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Consumer(() => new NullTaskResponseHandler());
        });

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendRequestAsync(
                new ResponseRequest(NewId.NextGuid()),
                new RequestTimeout(TimeSpan.FromSeconds(10)),
                TestContext.Current.CancellationToken));

        Assert.Equal(
            $"The mediator request handler '{typeof(NullTaskResponseHandler).FullName}' returned a null task.",
            failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-LIFECYCLE", "concurrent-disposal-single-flight-dual-fault")]
    public async Task DisposeAsync_ConcurrentCallersShareOneDualFaultedCleanupAsync()
    {
        var clientFailure = new InvalidOperationException("client cleanup failed");
        var observerFailure = new ApplicationException("observer cleanup failed");
        var clientContext = new ControlledClientFactoryContext();
        var observerHandle = new ControlledConnectHandle();
        var mediator = Assert.IsType<InProcessMediator>(MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative)));
        ClientFactory originalClientFactory = GetField<ClientFactory>(mediator, "_clientFactory");
        ConnectHandle originalObservers = GetField<ConnectHandle>(mediator, "_configuredConsumeObservers");
        await originalClientFactory.DisposeAsync();
        await ((IAsyncDisposable)originalObservers).DisposeAsync();
        SetField(mediator, "_clientFactory", new ClientFactory(clientContext));
        SetField(mediator, "_configuredConsumeObservers", observerHandle);

        Task<Task>[] callers = Enumerable.Range(0, 32)
            .Select(_ => Task.Run<Task>(
                () => mediator.DisposeAsync().AsTask(),
                TestContext.Current.CancellationToken))
            .ToArray();
        Task[] cleanupTasks = await Task.WhenAll(callers);

        Assert.All(cleanupTasks, cleanup => Assert.Same(cleanupTasks[0], cleanup));
        Assert.Equal(1, clientContext.DisposeCount);
        Assert.Equal(1, observerHandle.DisposeCount);

        clientContext.Fail(clientFailure);
        observerHandle.Fail(observerFailure);

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => cleanupTasks[0]);
        Assert.StartsWith("Mediator cleanup failed.", failure.Message, StringComparison.Ordinal);
        Assert.Collection(
            failure.InnerExceptions,
            exception => Assert.Same(clientFailure, exception),
            exception => Assert.Same(observerFailure, exception));

        Task repeatedCleanup = mediator.DisposeAsync().AsTask();
        Assert.Same(cleanupTasks[0], repeatedCleanup);
        AggregateException repeatedFailure = await Assert.ThrowsAsync<AggregateException>(() => repeatedCleanup);
        Assert.Same(failure, repeatedFailure);
        Assert.Equal(1, clientContext.DisposeCount);
        Assert.Equal(1, observerHandle.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-LIFECYCLE", "single-cleanup-failure-is-unwrapped")]
    public async Task DisposeAsync_PropagatesEitherSingleCleanupFailureWithoutWrappingAsync(bool clientFails)
    {
        var expected = new InvalidOperationException(clientFails ? "client failed" : "observer failed");
        var clientContext = new ControlledClientFactoryContext();
        var observerHandle = new ControlledConnectHandle();
        InProcessMediator mediator = await CreateMediatorWithCleanupOwnersAsync(clientContext, observerHandle);

        Task cleanup = mediator.DisposeAsync().AsTask();
        if (clientFails)
        {
            clientContext.Fail(expected);
            observerHandle.Complete();
        }
        else
        {
            clientContext.Complete();
            observerHandle.Fail(expected);
        }

        Exception failure = await Assert.ThrowsAnyAsync<Exception>(() => cleanup);
        Assert.Same(expected, failure);
        Assert.Equal(1, clientContext.DisposeCount);
        Assert.Equal(1, observerHandle.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-LIFECYCLE", "single-cleanup-cancellation-is-preserved")]
    public async Task DisposeAsync_PreservesCancellationFromEitherCleanupOwnerAsync(bool clientCancels)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var clientContext = new ControlledClientFactoryContext();
        var observerHandle = new ControlledConnectHandle();
        InProcessMediator mediator = await CreateMediatorWithCleanupOwnersAsync(clientContext, observerHandle);

        Task cleanup = mediator.DisposeAsync().AsTask();
        if (clientCancels)
        {
            clientContext.Cancel(source.Token);
            observerHandle.Complete();
        }
        else
        {
            clientContext.Complete();
            observerHandle.Cancel(source.Token);
        }

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cleanup);
        Assert.Equal(source.Token, failure.CancellationToken);
        Assert.True(cleanup.IsCanceled);
        Assert.Equal(1, clientContext.DisposeCount);
        Assert.Equal(1, observerHandle.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-LIFECYCLE", "configured-observer-second-connection-rollback")]
    public void ConnectConfiguredObservers_SecondConnectionFailureDisposesThePrimaryHandle()
    {
        var expected = new InvalidOperationException("response observer connection failed");
        var primaryHandle = new RollbackConnectHandle();
        var observer = new EmptyConsumeObserver();
        IReceivePipeDispatcher primary = CreateProxy<IReceivePipeDispatcher>((method, arguments) =>
        {
            Assert.Equal(nameof(IConsumeObserverConnector.ConnectConsumeObserver), method.Name);
            Assert.Same(observer, arguments![0]);
            return primaryHandle;
        });
        IReceivePipeDispatcher response = CreateProxy<IReceivePipeDispatcher>((method, arguments) =>
        {
            Assert.Equal(nameof(IConsumeObserverConnector.ConnectConsumeObserver), method.Name);
            Assert.Same(observer, arguments![0]);
            throw expected;
        });
        MethodInfo method = typeof(InProcessMediator).GetMethod(
            "ConnectConfiguredObservers",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var connect = (Func<IReceivePipeDispatcher, IReceivePipeDispatcher, IConsumeObserver, ConnectHandle>)method.CreateDelegate(
            typeof(Func<IReceivePipeDispatcher, IReceivePipeDispatcher, IConsumeObserver, ConnectHandle>));

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            connect(primary, response, observer));

        Assert.Same(expected, failure);
        Assert.Equal(1, primaryHandle.DisposeCount);
    }

    private static async Task<InProcessMediator> CreateMediatorWithCleanupOwnersAsync(
        ControlledClientFactoryContext clientContext,
        ControlledConnectHandle observerHandle)
    {
        var mediator = Assert.IsType<InProcessMediator>(MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative)));
        ClientFactory originalClientFactory = GetField<ClientFactory>(mediator, "_clientFactory");
        ConnectHandle originalObservers = GetField<ConnectHandle>(mediator, "_configuredConsumeObservers");
        await originalClientFactory.DisposeAsync();
        await ((IAsyncDisposable)originalObservers).DisposeAsync();
        SetField(mediator, "_clientFactory", new ClientFactory(clientContext));
        SetField(mediator, "_configuredConsumeObservers", observerHandle);
        return mediator;
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        T contract = DispatchProxy.Create<T, DelegatingProxy>();
        Assert.IsAssignableFrom<DelegatingProxy>(contract).InvokeMethod = invoke;
        return contract;
    }

    private static async Task AssertParameterAsync(string expected, Func<Task> action)
    {
        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private static T GetField<T>(InProcessMediator mediator, string name) where T : class =>
        Assert.IsAssignableFrom<T>(typeof(InProcessMediator)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(mediator));

    private static void SetField(InProcessMediator mediator, string name, object value) =>
        typeof(InProcessMediator)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(mediator, value);

    private sealed class NullTaskOneWayHandler : MediatorRequestHandler<OneWayRequest>
    {
        protected override Task HandleAsync(OneWayRequest request, CancellationToken cancellationToken) => null!;
    }

    private sealed class NullTaskResponseHandler : MediatorRequestHandler<ResponseRequest, ResponseMessage>
    {
        protected override Task<ResponseMessage> HandleAsync(ResponseRequest request, CancellationToken cancellationToken) => null!;
    }

    private sealed class ControlledClientFactoryContext : ClientFactoryContext, IAsyncDisposable
    {
        readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public RequestTimeout DefaultTimeout => throw new NotSupportedException();
        public TimeProvider TimeProvider => throw new NotSupportedException();
        public IMessageRouteTable MessageRoutes => throw new NotSupportedException();
        public Uri ResponseAddress => throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe) where T : class =>
            throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default) where T : class =>
            throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default) where T : class =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return new ValueTask(_completion.Task);
        }

        public void Fail(Exception exception) => _completion.TrySetException(exception);

        public void Complete() => _completion.TrySetResult();

        public void Cancel(CancellationToken cancellationToken) => _completion.TrySetCanceled(cancellationToken);
    }

    private sealed class ControlledConnectHandle : ConnectHandle
    {
        readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Disconnect()
        {
        }

        public void Dispose()
        {
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return new ValueTask(_completion.Task);
        }

        public void Fail(Exception exception) => _completion.TrySetException(exception);

        public void Complete() => _completion.TrySetResult();

        public void Cancel(CancellationToken cancellationToken) => _completion.TrySetCanceled(cancellationToken);
    }

    public class DelegatingProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> InvokeMethod { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => InvokeMethod(targetMethod!, args);
    }

    private sealed class FaultingRequestHandle(IRequest<ResponseMessage> request, RequestException failure) :
        RequestHandle<IRequest<ResponseMessage>>
    {
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public Task<IRequest<ResponseMessage>> Message => Task.FromResult(request);
        public Guid RequestId { get; } = NewId.NextGuid();
        public RequestTimeout TimeToLive { private get; set; }

        public Task<Response<T>> GetResponseAsync<T>(bool readyToSend = true, CancellationToken cancellationToken = default)
            where T : class => Task.FromException<Response<T>>(failure);

        public void AddPipeSpecification(IPipeSpecification<SendContext<IRequest<ResponseMessage>>> specification)
        {
        }

        public void Cancel()
        {
        }

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }

    private sealed class RollbackConnectHandle : ConnectHandle
    {
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Disconnect() => Dispose();

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }

    private sealed class EmptyConsumeObserver : IConsumeObserver
    {
        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    private sealed record OneWayRequest(string Value);
    private sealed record ResponseRequest(Guid CorrelationId) : IRequest<ResponseMessage>, ICorrelatedBy<Guid>;
    private sealed record ResponseMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    private sealed class PublishedMessage
    {
        public string Value { get; set; } = string.Empty;
    }
}
