using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureRequestDispatchContractTests
{
    private static readonly Uri DestinationAddress = new("loopback://localhost/future-request");

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-DISPATCH", "pending-identifier-precedes-dispatch")]
    public async Task TrackedRequest_AddsItsPendingIdentifierBeforeTransportDispatchAsync()
    {
        Guid requestId = Guid.Parse("808871af-6f48-479d-b484-8403af9da15c");
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var recorder = new OutgoingMessageRecorder(message =>
        {
            RequestMessage request = Assert.IsType<RequestMessage>(message);
            Assert.Equal(requestId, request.RequestId);
            Assert.Contains(requestId, state.Pending);
        });
        FutureRequest<InputMessage, RequestMessage> request = CreateRequest(requestId);

        var machine = new ContextMachine();
        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.InputReceived,
            state,
            new InputMessage("input"),
            context => request.SendRequestAsync(context, context.CancellationToken),
            recorder,
            TestContext.Current.CancellationToken);

        Assert.Equal([requestId], state.Pending);
        Assert.Single(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-DISPATCH", "failed-dispatch-rolls-back-pending-identifier")]
    public async Task TrackedRequest_RemovesItsPendingIdentifierWhenTransportDispatchFailsAsync()
    {
        Guid requestId = Guid.Parse("9cbe791c-2421-4fe7-bb8d-228e1ca10a15");
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var recorder = new OutgoingMessageRecorder(_ => throw new ExpectedDispatchException());
        FutureRequest<InputMessage, RequestMessage> request = CreateRequest(requestId);

        await Assert.ThrowsAsync<ExpectedDispatchException>(() =>
        {
            var machine = new ContextMachine();
            return FutureBehaviorContextFactory.UseAsync(
                machine,
                machine.InputReceived,
                state,
                new InputMessage("input"),
                context => request.SendRequestAsync(context, context.CancellationToken),
                recorder,
                TestContext.Current.CancellationToken);
        });

        Assert.Empty(state.Pending);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-DISPATCH", "duplicate-pending-identifier-prevents-dispatch")]
    public async Task TrackedRequest_RejectsAnAlreadyPendingIdentifierBeforeDispatchAsync()
    {
        Guid requestId = Guid.Parse("d03d24e3-54ca-459d-9a0d-ee2cf9160cfd");
        var state = new FutureState { CorrelationId = Guid.NewGuid(), Pending = [requestId] };
        var recorder = new OutgoingMessageRecorder();
        FutureRequest<InputMessage, RequestMessage> request = CreateRequest(requestId);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            var machine = new ContextMachine();
            return FutureBehaviorContextFactory.UseAsync(
                machine,
                machine.InputReceived,
                state,
                new InputMessage("input"),
                context => request.SendRequestAsync(context, context.CancellationToken),
                recorder,
                TestContext.Current.CancellationToken);
        });

        Assert.Contains(requestId.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal([requestId], state.Pending);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-DISPATCH", "context-selected-address-is-absolute")]
    public async Task Request_RejectsARelativeContextSelectedAddressBeforeDispatchAsync()
    {
        Guid requestId = Guid.Parse("2d27bfbb-c721-4327-a5ed-f2e1e4715ae9");
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var recorder = new OutgoingMessageRecorder();
        FutureRequest<InputMessage, RequestMessage> request = CreateRequest(requestId);
        request.AddressProvider = _ => new Uri("relative-request", UriKind.Relative);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            var machine = new ContextMachine();
            return FutureBehaviorContextFactory.UseAsync(
                machine,
                machine.InputReceived,
                state,
                new InputMessage("input"),
                context => request.SendRequestAsync(context, context.CancellationToken),
                recorder,
                TestContext.Current.CancellationToken);
        });

        Assert.Equal("AddressProvider", exception.ParamName);
        Assert.Empty(state.Pending);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-DISPATCH", "pending-identifier-is-nonempty")]
    public async Task TrackedRequest_RejectsAnEmptyPendingIdentifierBeforeDispatchAsync()
    {
        var state = new FutureState { CorrelationId = Guid.NewGuid() };
        var recorder = new OutgoingMessageRecorder();
        FutureRequest<InputMessage, RequestMessage> request = CreateRequest(Guid.Empty);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            var machine = new ContextMachine();
            return FutureBehaviorContextFactory.UseAsync(
                machine,
                machine.InputReceived,
                state,
                new InputMessage("input"),
                context => request.SendRequestAsync(context, context.CancellationToken),
                recorder,
                TestContext.Current.CancellationToken);
        });

        Assert.Contains("nonempty", exception.Message, StringComparison.Ordinal);
        Assert.Empty(state.Pending);
        Assert.Empty(recorder.Messages);
    }

    private static FutureRequest<InputMessage, RequestMessage> CreateRequest(Guid requestId)
    {
        var request = new FutureRequest<InputMessage, RequestMessage>
        {
            AddressProvider = _ => DestinationAddress,
            PendingRequestIdProvider = message => message.RequestId,
            Factory = new ContextMessageFactory<BehaviorContext<FutureState, InputMessage>, RequestMessage>(
                _ => Task.FromResult(new InitializedMessage<RequestMessage>(new RequestMessage(requestId)))),
        };
        return request;
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public Event<InputMessage> InputReceived { get; private set; } = null!;
    }

    public sealed record InputMessage(string Value);

    public sealed record RequestMessage(Guid RequestId);

    private sealed class ExpectedDispatchException : Exception;
}
