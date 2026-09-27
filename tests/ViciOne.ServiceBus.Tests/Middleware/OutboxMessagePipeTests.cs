using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class OutboxMessagePipeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "missing-destination-cannot-complete-delivery")]
    public async Task MissingDestination_FaultsWithoutAcknowledgingOrCompletingTheOutboxAsync()
    {
        var state = new DeliveryState(destination: null);
        Exception? failure = await Record.ExceptionAsync(() => state.Pipe.SendAsync(state.Context));

        InvalidOperationException rejected = Assert.IsType<InvalidOperationException>(failure);
        Assert.Contains("DestinationAddress", rejected.Message, StringComparison.Ordinal);
        Assert.Equal(0, state.CompletedCount);
        Assert.Equal(0, state.AcknowledgedCount);
        Assert.Equal(0, state.RemovedCount);
        Assert.Equal(0, state.ResolutionCount);
        Assert.Single(state.Messages);
        Assert.Same(state.Message, state.Messages[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-OUTBOX-RECOVERY", "resolution-observes-caller-and-delivery-cancellation")]
    public async Task EndpointResolution_ObservesDeliveryCancellationBeforeSendAsync(bool deliveryDeadline)
    {
        using var caller = new CancellationTokenSource();
        var state = new DeliveryState(new Uri("loopback://localhost/outgoing"), caller.Token,
            deliveryDeadline ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMinutes(1));
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        state.Resolve = async token =>
        {
            entered.TrySetResult(token);
            return await release.Task.WaitAsync(token);
        };
        Task operation = state.Pipe.SendAsync(state.Context);
        try
        {
            CancellationToken observed = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(observed.CanBeCanceled, "Endpoint resolution must receive the linked delivery token.");
            if (!deliveryDeadline)
                await caller.CancelAsync();

            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(observed, failure.CancellationToken);
            Assert.True(observed.IsCancellationRequested);
            Assert.Equal(!deliveryDeadline, caller.IsCancellationRequested);
            Assert.Equal(1, state.ResolutionCount);
            Assert.Equal(0, state.AcknowledgedCount);
            Assert.Equal(0, state.CompletedCount);
            Assert.Equal(0, state.RemovedCount);
            Assert.Same(state.Message, Assert.Single(state.Messages));
        }
        finally
        {
            release.TrySetCanceled(CancellationToken.None);
            Exception? cleanup = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            Assert.IsNotType<TimeoutException>(cleanup);
            await Record.ExceptionAsync(() => release.Task);
        }
    }

    public sealed record Input(Guid Id);

    private sealed class DeliveryState
    {
        public DeliveryState(Uri? destination, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        {
            Message = StrictProxy.Create<OutboxMessageContext>((method, _) => method.Name switch
            {
                "get_SequenceNumber" => 7L,
                "get_MessageId" => Guid.Parse("4927f624-e0f5-4ac4-a092-d9511a200bea"),
                "get_DestinationAddress" => destination,
                _ => throw new NotSupportedException(method.Name),
            });
            Messages = [Message];
            ConsumeContext captured = StrictProxy.Create<ConsumeContext>((method, args) =>
            {
                if (method.Name != "GetSendEndpointAsync")
                    throw new NotSupportedException(method.Name);
                Assert.Equal(destination, args![0]);
                ResolutionCount++;
                return Resolve((CancellationToken)args[1]!);
            });
            Context = StrictProxy.Create<OutboxConsumeContext<Input>>((method, args) =>
            {
                switch (method.Name)
                {
                    case "TryGetPayload":
                        args![0] = null;
                        return false;
                    case "get_IsMessageConsumed": return true;
                    case "get_IsOutboxDelivered": return false;
                    case "get_LastSequenceNumber": return null;
                    case "get_CancellationToken": return cancellationToken;
                    case "get_CapturedContext": return captured;
                    case "get_ConsumeCompleted": return Task.CompletedTask;
                    case "LoadOutboxMessagesAsync": return Task.FromResult(Messages);
                    case "SetDeliveredAsync": CompletedCount++; return Task.CompletedTask;
                    case "NotifyOutboxMessageDeliveredAsync": AcknowledgedCount++; return Task.CompletedTask;
                    case "RemoveOutboxMessagesAsync": RemovedCount++; return Task.CompletedTask;
                    default: throw new NotSupportedException(method.Name);
                }
            });
            var options = new OutboxConsumeOptions
            {
                ConsumerId = Guid.Parse("36d4ebd6-14cf-4e79-b268-0ae945eaaf03"),
                ConsumerType = nameof(Input),
                MessageDeliveryLimit = 2,
                MessageDeliveryTimeout = timeout ?? TimeSpan.FromSeconds(5),
            };
            Pipe = new OutboxMessagePipe<Input>(options, new Scope(Context),
                ViciOne.ServiceBus.Advanced.Middleware.Pipe.Execute<ConsumeContext<Input>>(_ =>
                    throw new InvalidOperationException("Committed consumption must not invoke the consumer again.")));
        }

        public OutboxMessagePipe<Input> Pipe { get; }
        public OutboxConsumeContext<Input> Context { get; }
        public OutboxMessageContext Message { get; }
        public List<OutboxMessageContext> Messages { get; }
        public Func<CancellationToken, Task<ISendEndpoint>> Resolve { get; set; } =
            _ => throw new InvalidOperationException("A missing destination must not resolve an endpoint.");
        public int ResolutionCount { get; private set; }
        public int CompletedCount { get; private set; }
        public int AcknowledgedCount { get; private set; }
        public int RemovedCount { get; private set; }
    }

    private sealed class Scope(ConsumeContext<Input> context) : IConsumeScopeContext<Input>, IDisposable
    {
        public ConsumeContext<Input> Context => context;
        public T GetService<T>() where T : class => throw new NotSupportedException();
        public T CreateInstance<T>(params object[] arguments) where T : class => throw new NotSupportedException();
        public IDisposable PushConsumeContext(ConsumeContext value) => this;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class StrictProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _invoke = null!;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
        {
            T instance = Create<T, StrictProxy>();
            ((StrictProxy)(object)instance)._invoke = invoke;
            return instance;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _invoke(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args);
    }
}
