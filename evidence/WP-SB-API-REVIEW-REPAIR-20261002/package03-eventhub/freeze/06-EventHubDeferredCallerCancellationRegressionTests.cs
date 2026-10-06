using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubDeferredCallerCancellationRegressionTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData("single")]
    [InlineData("batch")]
    [InlineData("values")]
    [InlineData("batch-values")]
    [InlineData("single-pipe")]
    [InlineData("batch-pipe")]
    [InlineData("values-pipe")]
    [InlineData("batch-values-pipe")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-DEFERRED-PRODUCER", "pre-canceled-caller-completes-synchronously-across-eight-routes")]
    public Task PreCanceledCaller_CompletesBeforeSharedResolutionAndPreservesLiveWaiterAsync(string route)
        => AssertCallerCancellationAsync(route, preCanceled: true);

    [Theory]
    [InlineData("single")]
    [InlineData("batch")]
    [InlineData("values")]
    [InlineData("batch-values")]
    [InlineData("single-pipe")]
    [InlineData("batch-pipe")]
    [InlineData("values-pipe")]
    [InlineData("batch-values-pipe")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-DEFERRED-PRODUCER", "later-canceled-caller-completes-before-shared-resolution-across-eight-routes")]
    public Task LaterCanceledCaller_CompletesBeforeSharedResolutionAndPreservesLiveWaiterAsync(string route)
        => AssertCallerCancellationAsync(route, preCanceled: false);

    private static async Task AssertCallerCancellationAsync(string route, bool preCanceled)
    {
        await using ConsumeLease lease = await ConsumeLease.CreateAsync();
        var provider = new DeferredProvider();
        var wrapper = new ConsumeContextEventHubProducerProvider(provider, lease.Context);
        var address = new Uri("topic:caller-cancellation");
        using var caller = new CancellationTokenSource();
        IEventHubProducer producer = await wrapper.GetProducerAsync(address, CancellationToken.None);
        IEventHubProducer downstream = DispatchProxy.Create<IEventHubProducer, DeliveryProxy>();
        var delivery = (DeliveryProxy)(object)downstream;
        int canceledPipeCalls = 0;
        if (preCanceled) await caller.CancelAsync();
        Task canceled = InvokeRoute(producer, route,
            Pipe.Execute<EventHubSendContext<Output>>(_ => Interlocked.Increment(ref canceledPipeCalls)), caller.Token).Pending;
        Task live = InvokeRoute(producer, route, Pipe.Empty<EventHubSendContext<Output>>(), TestContext.Current.CancellationToken).Pending;
        try
        {
            // WaitAsync with an already canceled token must cancel the public async
            // wrapper before it returns. This is causal on the original source
            // without waiting for the operation deadline.
            if (preCanceled) Assert.True(canceled.IsCanceled);
            else Assert.False(canceled.IsCompleted);
            Assert.False(live.IsCompleted);
            if (!preCanceled) await caller.CancelAsync();
            OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                canceled.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(caller.Token, error.CancellationToken);
            Assert.True(canceled.IsCanceled);
            Assert.False(provider.Ready.Task.IsCompleted);
            Assert.False(live.IsCompleted);
            Assert.Equal(0, delivery.Calls);
            Assert.Equal(0, canceledPipeCalls);
            Assert.Equal(1, provider.Calls);
            Assert.Equal(CancellationToken.None, provider.Token);
            Assert.Equal(address, provider.Address);
            provider.Ready.SetResult(downstream);
            await delivery.Entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            delivery.Completed.SetResult();
            await live.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.True(provider.Ready.Task.IsCompletedSuccessfully);
            Assert.Equal(1, delivery.Calls);
            Assert.Equal(TestContext.Current.CancellationToken, delivery.Token);
            Assert.Equal(0, canceledPipeCalls);
            Assert.True(canceled.IsCanceled);
        }
        finally
        {
            provider.Ready.TrySetResult(downstream);
            delivery.Completed.TrySetResult();
            try { await Task.WhenAll(ObserveTerminationAsync(canceled), ObserveTerminationAsync(live)); }
            finally { if (delivery.Operation is not null) await ObserveTerminationAsync(delivery.Operation); }
        }
    }

    [Theory]
    [InlineData("single", false)]
    [InlineData("batch", false)]
    [InlineData("values", false)]
    [InlineData("batch-values", false)]
    [InlineData("single-pipe", false)]
    [InlineData("batch-pipe", false)]
    [InlineData("values-pipe", false)]
    [InlineData("batch-values-pipe", false)]
    [InlineData("single", true)]
    [InlineData("batch", true)]
    [InlineData("values", true)]
    [InlineData("batch-values", true)]
    [InlineData("single-pipe", true)]
    [InlineData("batch-pipe", true)]
    [InlineData("values-pipe", true)]
    [InlineData("batch-values-pipe", true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-DEFERRED-PRODUCER", "ready-and-pending-positive-eight-routes-await-provider-delivery")]
    public async Task PositiveRoutes_ForwardExactlyOnceAndAwaitDeliveryAsync(string route, bool alreadyResolved)
    {
        await using ConsumeLease lease = await ConsumeLease.CreateAsync();
        var provider = new DeferredProvider();
        IEventHubProducer downstream = DispatchProxy.Create<IEventHubProducer, DeliveryProxy>();
        var delivery = (DeliveryProxy)(object)downstream;
        if (alreadyResolved) provider.Ready.SetResult(downstream);
        var wrapper = new ConsumeContextEventHubProducerProvider(provider, lease.Context);
        IEventHubProducer producer = await wrapper.GetProducerAsync(new Uri("topic:positive-route"), CancellationToken.None);
        int pipeCalls = 0;
        RouteInvocation invocation = InvokeRoute(producer, route,
            Pipe.Execute<EventHubSendContext<Output>>(_ => Interlocked.Increment(ref pipeCalls)), TestContext.Current.CancellationToken);
        Task operation = invocation.Pending;
        try
        {
            if (!alreadyResolved)
            {
                Assert.False(operation.IsCompleted);
                Assert.Equal(0, delivery.Calls);
                provider.Ready.SetResult(downstream);
            }
            await delivery.Entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, delivery.Calls);
            Assert.Equal(TestContext.Current.CancellationToken, delivery.Token);
            Assert.Equal(route.EndsWith("-pipe", StringComparison.Ordinal) ? 1 : 0, pipeCalls);
            Assert.Equal(typeof(Output), delivery.MessageType);
            Assert.Equal(invocation.ExpectedFormalType, delivery.InputType);
            Assert.Same(invocation.ExpectedArgument, delivery.Input);
            AssertArgumentContents(route, delivery.Input);
            EventHubMessageSendContext<Output> observed = Assert.IsType<EventHubMessageSendContext<Output>>(delivery.Context);
            Assert.Same(lease.Context, observed.GetPayload<ConsumeContext>());
            Assert.Equal(lease.ConversationId, observed.ConversationId);
            Assert.Equal(lease.CorrelationId, observed.InitiatorId);
            Assert.Equal("incoming", observed.Headers.Get<string>("Application"));
            delivery.Completed.SetResult();
            await operation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(1, delivery.Calls);
        }
        finally
        {
            provider.Ready.TrySetResult(downstream);
            delivery.Completed.TrySetResult();
            try { await ObserveTerminationAsync(operation); }
            finally { if (delivery.Operation is not null) await ObserveTerminationAsync(delivery.Operation); }
        }
    }

    private sealed record RouteInvocation(Task Pending, object ExpectedArgument, Type ExpectedFormalType);

    private static RouteInvocation InvokeRoute(IEventHubProducer producer, string route, IPipe<EventHubSendContext<Output>> pipe, CancellationToken token)
    {
        var message = new Output(17, "typed");
        IEnumerable<Output> messages = new[] { message };
        object values = new InitializationValues(17, "initialized");
        IEnumerable<object> batchValues = new[] { values };
        Task pending = route switch
        {
            "single" => producer.ProduceAsync(message, token),
            "batch" => producer.ProduceAsync(messages, token),
            "values" => producer.ProduceAsync<Output>(values, token),
            "batch-values" => producer.ProduceAsync<Output>(batchValues, token),
            "single-pipe" => producer.ProduceAsync(message, pipe, token),
            "batch-pipe" => producer.ProduceAsync(messages, pipe, token),
            "values-pipe" => producer.ProduceAsync<Output>(values, pipe, token),
            "batch-values-pipe" => producer.ProduceAsync<Output>(batchValues, pipe, token),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        (object argument, Type formal) = route.Replace("-pipe", "", StringComparison.Ordinal) switch
        {
            "single" => (message, typeof(Output)),
            "batch" => (messages, typeof(IEnumerable<Output>)),
            "values" => (values, typeof(object)),
            "batch-values" => (batchValues, typeof(IEnumerable<object>)),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        return new RouteInvocation(pending, argument, formal);
    }

    private static void AssertArgumentContents(string route, object? input)
    {
        switch (route.Replace("-pipe", "", StringComparison.Ordinal))
        {
            case "single": Assert.Equal(new Output(17, "typed"), Assert.IsType<Output>(input)); break;
            case "batch": Assert.Equal(new Output(17, "typed"), Assert.Single(Assert.IsAssignableFrom<IEnumerable<Output>>(input))); break;
            case "values": Assert.Equal(new InitializationValues(17, "initialized"), Assert.IsType<InitializationValues>(input)); break;
            case "batch-values": Assert.Equal(new InitializationValues(17, "initialized"), Assert.IsType<InitializationValues>(Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(input)))); break;
            default: throw new ArgumentOutOfRangeException(nameof(route));
        }
    }

    private static async Task ObserveTerminationAsync(Task operation)
    {
        await Task.WhenAny(operation).WaitAsync(Timeout, CancellationToken.None);
        _ = await Record.ExceptionAsync(() => operation); // Already joined; body owns the asserted outcome.
    }

    public sealed record Output(int Number, string Text);
    public sealed record InitializationValues(int Number, string Text);
    private sealed record Input(Guid Id);

    private sealed class DeferredProvider : IEventHubProducerProvider
    {
        public TaskCompletionSource<IEventHubProducer> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Uri? Address { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Calls++;
            Address = address;
            Token = cancellationToken;
            return Ready.Task;
        }
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }

    public class DeliveryProxy : DispatchProxy
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public object? Input { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public EventHubMessageSendContext<Output>? Context { get; private set; }
        public Type? InputType { get; private set; }
        public Type? MessageType { get; private set; }
        public Task? Operation { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IEventHubProducer.ProduceAsync) || args is not { Length: 3 })
                throw new InvalidOperationException($"Unexpected producer operation: {targetMethod?.Name}");
            Calls++;
            InputType = targetMethod.GetParameters()[0].ParameterType;
            MessageType = Assert.Single(targetMethod.GetGenericArguments());
            Input = args[0];
            Token = (CancellationToken)args[2]!;
            Operation = DeliverAsync((IPipe<EventHubSendContext<Output>>)args[1]!);
            return Operation;
        }

        private async Task DeliverAsync(IPipe<EventHubSendContext<Output>> pipe)
        {
            try
            {
                Context = new EventHubMessageSendContext<Output>(new Output(17, "provider context"), Token);
                await pipe.SendAsync(Context);
                Entered.TrySetResult();
                await Completed.Task;
            }
            catch (Exception exception)
            {
                Entered.TrySetException(exception);
                throw;
            }
        }
    }

    private sealed class ConsumeLease : IAsyncDisposable
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ConsumeContext> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IBusControl _bus = null!;
        public ConsumeContext Context { get; private set; } = null!;
        public Guid ConversationId { get; } = Guid.NewGuid();
        public Guid CorrelationId { get; } = Guid.NewGuid();

        public static async Task<ConsumeLease> CreateAsync()
        {
            var lease = new ConsumeLease();
            string queue = $"deferred-{Guid.NewGuid():N}";
            lease._bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(queue,
                endpoint => endpoint.Handler<Input>(async context =>
                {
                    lease._received.TrySetResult(context.Advanced());
                    await lease._release.Task;
                })));
            CancellationToken token = TestContext.Current.CancellationToken;
            try
            {
                await lease._bus.StartAsync(token).WaitAsync(Timeout, token);
                ISendEndpoint endpoint = await lease._bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), token);
                await endpoint.SendAsync(new Input(Guid.NewGuid()), context =>
                {
                    context.ConversationId = lease.ConversationId;
                    context.CorrelationId = lease.CorrelationId;
                    context.Headers.Set("Application", "incoming");
                }, token);
                lease.Context = await lease._received.Task.WaitAsync(Timeout, token);
                return lease;
            }
            catch
            {
                await lease.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _release.TrySetResult();
            await _bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }
}
