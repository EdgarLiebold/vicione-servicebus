using System.Collections.Concurrent;
using ViciOne.ServiceBus.Clients;
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
    public async Task RequestSendEndpoint_BypassesADeferredEndpointBeforeSending()
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
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<InnerRequest>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });

        await harness.Start(cancellationToken);
        try
        {
            var outbox = new RecordingOutboxContext();
            var deferredEndpoint = new InMemoryOutboxSendEndpoint(
                outbox,
                harness.InputQueueSendEndpoint);
            var requestEndpoint = new FixedRequestSendEndpoint(deferredEndpoint);
            Guid correlationId = Guid.Parse("4ee2eaa1-3348-45ad-bb67-e76b3e4517d2");

            await requestEndpoint.Send(
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
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-OUTBOX", "nested-request-bypasses-deferred-delivery")]
    public async Task NestedRequest_BypassesTheOutboxWhileIndependentSideEffectsRemainDeferred()
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
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseInMemoryOutbox();
            configurator.Consumer(() => new OuterConsumer(harness.Bus, events));
            configurator.Consumer(() => new InnerConsumer(events));
            configurator.Handler<DeferredSideEffect>(context =>
            {
                events.Enqueue("side-effect-consumed");
                sideEffectSeen.TrySetResult(context);
                return Task.CompletedTask;
            });
        };

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("aed6e056-e9db-433b-af42-a048046b3912");
            IRequestClient<OuterRequest> client =
                harness.Bus.CreateRequestClient<OuterRequest>(harness.InputQueueAddress, timeout);

            Response<OuterResponse> response = await client.GetResponse<OuterResponse>(
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
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record OuterRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record InnerRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record InnerResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record OuterResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record DeferredSideEffect(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class FixedRequestSendEndpoint(ISendEndpoint endpoint) :
        RequestSendEndpoint<InnerRequest>(consumeContext: null)
    {
        protected override Task<ISendEndpoint> GetSendEndpoint() => Task.FromResult(endpoint);
    }

    private sealed class RecordingOutboxContext : OutboxContext
    {
        private int _deferredSendCount;

        public int DeferredSendCount => Volatile.Read(ref _deferredSendCount);

        public Task ClearToSend => Task.CompletedTask;

        public Task Add(Func<Task> method)
        {
            Interlocked.Increment(ref _deferredSendCount);
            return Task.CompletedTask;
        }

        public OutboxCheckpoint CreateCheckpoint() =>
            throw new NotSupportedException("This endpoint-bypass probe never checkpoints its recording outbox.");

        public Task ExecutePendingActions(bool concurrentMessageDelivery) => Task.CompletedTask;

        public Task DiscardPendingActions() => Task.CompletedTask;

        public Task DiscardPendingActions(OutboxCheckpoint checkpoint) =>
            throw new NotSupportedException("This endpoint-bypass probe never rolls back its recording outbox.");
    }

    private sealed class OuterConsumer(
        IBus bus,
        ConcurrentQueue<string> events) : IConsumer<OuterRequest>
    {
        public async Task Consume(ConsumeContext<OuterRequest> context)
        {
            events.Enqueue("outer-start");
            await context.Publish(
                new DeferredSideEffect(context.Message.CorrelationId),
                context.CancellationToken);

            IRequestClient<InnerRequest> client = context.CreateRequestClient<InnerRequest>(
                bus,
                context.ReceiveContext.InputAddress,
                RequestTimeout.After(s: 1));
            Response<InnerResponse> inner = await client.GetResponse<InnerResponse>(
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
        public async Task Consume(ConsumeContext<InnerRequest> context)
        {
            events.Enqueue("inner-consumed");
            await context.RespondAsync(new InnerResponse(
                context.Message.CorrelationId,
                "inner-complete"));
        }
    }
}
