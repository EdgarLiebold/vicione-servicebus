using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubDeferredProducerTests
{
    [Theory]
    [InlineData("single", false)]
    [InlineData("batch", false)]
    [InlineData("values", false)]
    [InlineData("batch-values", false)]
    [InlineData("single", true)]
    [InlineData("batch", true)]
    [InlineData("values", true)]
    [InlineData("batch-values", true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-DEFERRED-PRODUCER", "resolution-and-delivery-gates-preserve-envelope-and-overrides")]
    public async Task DeferredResolution_PreservesEnvelopeAndAwaitsDeliveryAsync(string route, bool deliveryFails)
    {
        await using ConsumeLease lease = await ConsumeLease.CreateAsync();
        var resolution = new DeferredProvider();
        var wrapper = new ConsumeContextEventHubProducerProvider(resolution, lease.Context);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var address = new Uri("topic:deferred-contract");
        IEventHubProducer producer = await wrapper.GetProducerAsync(address, cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        IEventHubProducer downstream = DispatchProxy.Create<IEventHubProducer, DeliveryProxy>();
        var delivery = (DeliveryProxy)(object)downstream;
        object input = CreateInput(route);
        int pipeCalls = 0;
        var pipe = Pipe.Execute<EventHubSendContext<Output>>(context =>
        {
            Assert.Equal(lease.ConversationId, context.ConversationId);
            Assert.Equal(lease.CorrelationId, context.InitiatorId);
            Assert.Equal("incoming", context.Headers.Get<string>("Application"));
            context.Headers.Set("Application", "outgoing-override");
            context.PartitionKey = "explicit-partition";
            Interlocked.Increment(ref pipeCalls);
        });
        Task pending = Produce(producer, route, input, pipe, cancellationToken);
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(0, delivery.Calls);
            Assert.Equal(0, pipeCalls);
            Assert.Equal(address, resolution.Address);
            Assert.Equal(cancellationToken, resolution.Token);

            resolution.Ready.SetResult(downstream);
            await delivery.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            Assert.False(pending.IsCompleted);
            Assert.Same(input, delivery.Input);
            Type expectedParameter = route switch
            {
                "single" => typeof(Output),
                "batch" => typeof(IEnumerable<Output>),
                "values" => typeof(object),
                "batch-values" => typeof(IEnumerable<object>),
                _ => throw new ArgumentOutOfRangeException(nameof(route)),
            };
            Assert.Equal(expectedParameter, delivery.InputType);
            Assert.Equal(typeof(Output), delivery.MessageType);
            Assert.Equal(cancellationToken, delivery.Token);
            Assert.Equal(1, delivery.Calls);
            Assert.Equal(1, pipeCalls);
            EventHubMessageSendContext<Output> observed = Assert.IsType<EventHubMessageSendContext<Output>>(delivery.Context);
            Assert.Equal(lease.Context.ReceiveContext.InputAddress, observed.SourceAddress);
            Assert.Equal(lease.ConversationId, observed.ConversationId);
            Assert.Equal(lease.CorrelationId, observed.InitiatorId);
            Assert.Same(lease.Context, observed.GetPayload<ConsumeContext>());
            Assert.Equal("outgoing-override", observed.Headers.Get<string>("Application"));
            Assert.Equal("explicit-partition", observed.PartitionKey);
            if (deliveryFails)
            {
                var expected = new InvalidOperationException("expected deferred provider send failure");
                delivery.Completed.SetException(expected);
                Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    pending.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken)));
            }
            else
            {
                delivery.Completed.SetResult();
                await pending.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            Assert.Equal(1, delivery.Calls);
        }
        finally
        {
            resolution.Ready.TrySetCanceled(cancellationToken);
            delivery.Completed.TrySetResult();
            await ObserveTerminationAsync(pending);
            if (delivery.Operation is not null)
                await ObserveTerminationAsync(delivery.Operation);
        }
    }

    [Theory]
    [InlineData("single", false)]
    [InlineData("batch", false)]
    [InlineData("values", false)]
    [InlineData("batch-values", false)]
    [InlineData("single", true)]
    [InlineData("batch", true)]
    [InlineData("values", true)]
    [InlineData("batch-values", true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-DEFERRED-PRODUCER", "failed-or-canceled-resolution-has-no-pipe-effects")]
    public async Task DeferredFailure_PropagatesWithoutSendEffectsAsync(string route, bool canceled)
    {
        await using ConsumeLease lease = await ConsumeLease.CreateAsync();
        var resolution = new DeferredProvider();
        var wrapper = new ConsumeContextEventHubProducerProvider(resolution, lease.Context);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IEventHubProducer producer = await wrapper.GetProducerAsync(new Uri("topic:failed-resolution"), cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        int pipeCalls = 0;
        Task pending = Produce(producer, route, CreateInput(route),
            Pipe.Execute<EventHubSendContext<Output>>(_ => Interlocked.Increment(ref pipeCalls)), cancellationToken);
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.Equal(0, pipeCalls);
            if (canceled)
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                resolution.Ready.SetCanceled(cancellation.Token);
                OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pending.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken));
                Assert.Equal(cancellation.Token, failure.CancellationToken);
            }
            else
            {
                var expected = new InvalidOperationException("expected deferred resolution failure");
                resolution.Ready.SetException(expected);
                Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    pending.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken)));
            }
            Assert.Equal(0, pipeCalls);
            Assert.Equal(1, resolution.Calls);
        }
        finally
        {
            resolution.Ready.TrySetCanceled(cancellationToken);
            await ObserveTerminationAsync(pending);
        }
    }

    private static async Task ObserveTerminationAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        catch (Exception) when (operation.IsCompleted)
        {
            // The test body asserts the outcome; cleanup observes an already terminal task.
        }
    }

    private static object CreateInput(string route) => route switch
    {
        "single" => new Output(17, "typed"),
        "batch" => new[] { new Output(17, "first"), new Output(23, "second") },
        "values" => new { Number = 17, Text = "initialized" },
        "batch-values" => new object[] { new { Number = 17, Text = "first" }, new { Number = 23, Text = "second" } },
        _ => throw new ArgumentOutOfRangeException(nameof(route)),
    };

    private static Task Produce(IEventHubProducer producer, string route, object input,
        IPipe<EventHubSendContext<Output>> pipe, CancellationToken token) => route switch
        {
            "single" => producer.ProduceAsync((Output)input, pipe, token),
            "batch" => producer.ProduceAsync((IEnumerable<Output>)input, pipe, token),
            "values" => producer.ProduceAsync<Output>(input, pipe, token),
            "batch-values" => producer.ProduceAsync<Output>((IEnumerable<object>)input, pipe, token),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };

    public sealed record Output(int Number, string Text);
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
                await lease._bus.StartAsync(token).WaitAsync(TimeSpan.FromSeconds(30), token);
                ISendEndpoint endpoint = await lease._bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), token);
                await endpoint.SendAsync(new Input(Guid.NewGuid()), context =>
                {
                    context.ConversationId = lease.ConversationId;
                    context.CorrelationId = lease.CorrelationId;
                    context.Headers.Set("Application", "incoming");
                }, token);
                lease.Context = await lease._received.Task.WaitAsync(TimeSpan.FromSeconds(30), token);
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
            await _bus.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        }
    }
}
