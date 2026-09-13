using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureTerminalDispatchContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "state-machine-completion-forwards-consume-cancellation")]
    public async Task TrackedRequestCompletion_ForwardsConsumeCancellationToTerminalDispatchAsync()
    {
        using var source = new CancellationTokenSource();
        var machine = new CancellationProbeFuture(source);
        Guid correlationId = Guid.NewGuid();
        Guid requestId = Guid.NewGuid();
        var state = new FutureState { CorrelationId = correlationId };
        var commandRecorder = new OutgoingMessageRecorder();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.CommandReceived,
            state,
            new ProbeCommand(correlationId, requestId),
            context => ((StateMachine<FutureState>)machine).RaiseEventAsync(context),
            commandRecorder,
            cancellationToken: TestContext.Current.CancellationToken,
            responseAddress: new Uri("loopback://localhost/future-terminal-cancellation"),
            requestId: Guid.NewGuid());

        Assert.Single(commandRecorder.Messages);
        Assert.Equal([requestId], state.Pending);
        var resultRecorder = new OutgoingMessageRecorder();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.ResponseReceived,
            state,
            new ProbeResponse(requestId),
            context => ((StateMachine<FutureState>)machine).RaiseEventAsync(context),
            resultRecorder,
            source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(source.IsCancellationRequested);
        Assert.False(state.Results.ContainsKey(state.CorrelationId));
        Assert.Empty(resultRecorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "failed-terminal-result-dispatch-rolls-back-lifecycle")]
    public async Task ResultDispatchFailure_RestoresPendingAndCompletionStateAsync()
    {
        var machine = new ContextMachine();
        var state = CreateSubscribedState();
        var result = new FutureResult<Command, ResultMessage, Signal>
        {
            Factory = CreateResultFactory(),
        };
        var recorder = new OutgoingMessageRecorder(_ => throw new ExpectedDispatchException());

        await Assert.ThrowsAsync<ExpectedDispatchException>(() => FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context => result.SetResultAsync(context, context.CancellationToken),
            recorder,
            TestContext.Current.CancellationToken));

        Assert.Equal([state.CorrelationId], state.Pending);
        Assert.Null(state.Completed);
        Assert.Empty(state.Results);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-FAULTS", "failed-terminal-fault-dispatch-rolls-back-lifecycle")]
    public async Task FaultDispatchFailure_RestoresPendingAndFaultStateAsync()
    {
        var machine = new ContextMachine();
        var state = CreateSubscribedState();
        var fault = new FutureFault<Command, FaultMessage, Signal>
        {
            Factory = CreateFaultFactory(),
        };
        var recorder = new OutgoingMessageRecorder(_ => throw new ExpectedDispatchException());

        await Assert.ThrowsAsync<ExpectedDispatchException>(() => FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context => fault.TrySetFaultedAsync(context, context.CancellationToken),
            recorder,
            TestContext.Current.CancellationToken));

        Assert.Equal([state.CorrelationId], state.Pending);
        Assert.Null(state.Faulted);
        Assert.Empty(state.Faults);
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-FAULTS", "deferred-terminal-fault-does-not-mutate-state")]
    public async Task DeferredFault_ReturnsFalseWithoutCreatingOrDispatchingAFaultAsync()
    {
        var machine = new ContextMachine();
        var state = CreateSubscribedState();
        bool factoryInvoked = false;
        var fault = new FutureFault<Command, FaultMessage, Signal>
        {
            WaitForPending = true,
            Factory = new ContextMessageFactory<BehaviorContext<FutureState, Signal>, FaultMessage>(_ =>
            {
                factoryInvoked = true;
                return Task.FromResult(new InitializedMessage<FaultMessage>(new FaultMessage("unexpected")));
            }),
        };
        var recorder = new OutgoingMessageRecorder();
        bool emitted = true;

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context => emitted = await fault.TrySetFaultedAsync(context, context.CancellationToken),
            recorder,
            TestContext.Current.CancellationToken);

        Assert.False(emitted);
        Assert.False(factoryInvoked);
        Assert.Equal([state.CorrelationId], state.Pending);
        Assert.Null(state.Faulted);
        Assert.Empty(state.Faults);
        Assert.Empty(recorder.Messages);
    }

    private static FutureState CreateSubscribedState()
    {
        Guid correlationId = Guid.NewGuid();
        return new FutureState
        {
            CorrelationId = correlationId,
            Pending = [correlationId],
            Subscriptions = [new FutureSubscription(new Uri("loopback://localhost/future-terminal"))],
        };
    }

    private static ContextMessageFactory<BehaviorContext<FutureState, Signal>, ResultMessage> CreateResultFactory()
    {
        return new ContextMessageFactory<BehaviorContext<FutureState, Signal>, ResultMessage>(
            context => Task.FromResult(new InitializedMessage<ResultMessage>(new ResultMessage(context.Message.Value))));
    }

    private static ContextMessageFactory<BehaviorContext<FutureState, Signal>, FaultMessage> CreateFaultFactory()
    {
        return new ContextMessageFactory<BehaviorContext<FutureState, Signal>, FaultMessage>(
            context => Task.FromResult(new InitializedMessage<FaultMessage>(new FaultMessage(context.Message.Value))));
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public Event<Signal> SignalReceived { get; private set; } = null!;
    }

    private sealed class CancellationProbeFuture : Future<ProbeCommand, ResultMessage>
    {
        public CancellationProbeFuture(CancellationTokenSource source)
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            ResponseReceived = SendRequest<ProbeRequest>(configuration =>
                {
                    configuration.SetRequestFactory(context => new ProbeRequest(context.Message.RequestId));
                    configuration.TrackPendingRequest(request => request.RequestId);
                })
                .OnResponseReceived<ProbeResponse>(configuration =>
                    configuration.CompletePendingRequest(response => response.RequestId))
                .Completed;
            WhenAllCompleted(configuration => configuration.SetResultFactory(_ =>
            {
                source.Cancel();
                return new ResultMessage("cancelled");
            }));
        }

        public Event<ProbeResponse> ResponseReceived { get; }
    }

    public sealed record Signal(string Value);

    public sealed record ProbeCommand(Guid CorrelationId, Guid RequestId) : CorrelatedBy<Guid>;

    public sealed record ProbeRequest(Guid RequestId);

    public sealed record ProbeResponse(Guid RequestId);

    public sealed record Command(string Value);

    public sealed record ResultMessage(string Value);

    public sealed record FaultMessage(string Value);

    private sealed class ExpectedDispatchException : Exception;
}
