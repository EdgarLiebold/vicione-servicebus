using System.Collections.Concurrent;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Endpoints;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using InMemoryOutboxSendEndpoint = ViciOne.ServiceBus.Middleware.InMemoryOutbox.OutboxSendEndpoint;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class RequestClientOutboxTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-OUTBOX", "all-request-endpoints-bypass-deferred-delivery")]
    public async Task RequestSendEndpoint_BypassesADeferredEndpointBeforeSendingAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"request-endpoint-outbox-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumed = new TaskCompletionSource<ConsumeContext<InnerRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<InnerRequest>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });

        await harness.StartAsync(cancellationToken);
        try
        {
            var outbox = new RecordingOutboxContext();
            var deferredEndpoint = new InMemoryOutboxSendEndpoint(
                outbox,
                harness.InputQueueSendEndpoint);
            var requestEndpoint = new FixedRequestSendEndpoint(deferredEndpoint);
            Guid correlationId = Guid.Parse("4ee2eaa1-3348-45ad-bb67-e76b3e4517d2");

            await requestEndpoint.SendAsync(
                NewId.NextGuid(),
                new InnerRequest(correlationId),
                Pipe.Empty<SendContext<InnerRequest>>(),
                cancellationToken);

            Assert.Equal(0, outbox.DeferredSendCount);
            ConsumeContext<InnerRequest> received =
                await consumed.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, received.Message.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-OUTBOX", "nested-request-bypasses-deferred-delivery")]
    public async Task NestedRequest_BypassesTheOutboxWhileIndependentSideEffectsRemainDeferredAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"request-outbox-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var events = new ConcurrentQueue<string>();
        var sideEffectSeen = new TaskCompletionSource<ConsumeContext<DeferredSideEffect>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
        {
            configurator.UseVolatileOutbox();
            configurator.Consumer(() => new OuterConsumer(harness.Bus, events));
            configurator.Consumer(() => new InnerConsumer(events));
            configurator.Handler<DeferredSideEffect>(context =>
            {
                events.Enqueue("side-effect-consumed");
                sideEffectSeen.TrySetResult(context);
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("aed6e056-e9db-433b-af42-a048046b3912");
            IRequestClient<OuterRequest> client =
                harness.Bus.CreateRequestClient<OuterRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<OuterResponse> response = await client.GetResponseAsync<OuterResponse>(
                new OuterRequest(correlationId),
                cancellationToken);
            ConsumeContext<DeferredSideEffect> sideEffect =
                await sideEffectSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new OuterResponse(correlationId, "inner-complete"), response.Message);
            Assert.Equal(correlationId, sideEffect.Message.CorrelationId);
            Assert.Equal(
                [
                    "outer-start",
                    "inner-consumed",
                    "outer-after-inner",
                    "outer-response-sent",
                    "side-effect-consumed",
                ],
                events);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record OuterRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record InnerRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record InnerResponse(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record OuterResponse(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record DeferredSideEffect(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class FixedRequestSendEndpoint(ISendEndpoint endpoint) :
        RequestSendEndpoint<InnerRequest>(consumeContext: null)
    {
        protected override Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(endpoint);
        }
    }

    private sealed class RecordingOutboxContext : OutboxContext
    {
        private int _deferredSendCount;

        public int DeferredSendCount => Volatile.Read(ref _deferredSendCount);

        public Task ClearToSend => Task.CompletedTask;

        public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Interlocked.Increment(ref _deferredSendCount);
            return Task.CompletedTask;
        }

        public OutboxCheckpoint CreateCheckpoint() =>
            throw new NotSupportedException("This endpoint-bypass probe never checkpoints its recording outbox.");

        public Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new NotSupportedException("This endpoint-bypass probe never rolls back its recording outbox."); }
    }

    private sealed class OuterConsumer(
        IBus bus,
        ConcurrentQueue<string> events) : IConsumer<OuterRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<OuterRequest> context)
        {
            events.Enqueue("outer-start");
            await context.Advanced().PublishAsync(
                new DeferredSideEffect(context.Message.CorrelationId),
                context.CancellationToken);

            IRequestClient<InnerRequest> client = context.Advanced().CreateRequestClient<InnerRequest>(
                bus,
                context.Advanced().ReceiveContext.InputAddress,
                new RequestTimeout(TimeSpan.FromSeconds(1)));
            Response<InnerResponse> inner = await client.GetResponseAsync<InnerResponse>(
                new InnerRequest(context.Message.CorrelationId),
                context.CancellationToken);

            events.Enqueue("outer-after-inner");
            await context.RespondAsync(new OuterResponse(
                inner.Message.CorrelationId,
                inner.Message.Value));
            events.Enqueue("outer-response-sent");
        }
    }

    private sealed class InnerConsumer(ConcurrentQueue<string> events) : IConsumer<InnerRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<InnerRequest> context)
        {
            events.Enqueue("inner-consumed");
            await context.RespondAsync(new InnerResponse(
                context.Message.CorrelationId,
                "inner-complete"));
        }
    }
}
