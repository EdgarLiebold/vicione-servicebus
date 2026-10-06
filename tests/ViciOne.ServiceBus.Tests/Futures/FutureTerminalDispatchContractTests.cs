using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureTerminalDispatchContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "tracked-response-membership-and-terminal-replay-preserve-result")]
    public async Task TrackedResponses_PreserveMembershipAndStoredOutcomeAcrossReplayAsync(bool replayFirst)
    {
        var machine = new MembershipFuture();
        Guid futureId = Guid.Parse("018cc251-f400-7000-8000-000000000501");
        Guid firstId = Guid.Parse("018cc251-f400-7000-8000-000000000502");
        Guid secondId = Guid.Parse("018cc251-f400-7000-8000-000000000503");
        var state = new FutureState { CorrelationId = futureId };
        var commands = new OutgoingMessageRecorder();
        await FutureBehaviorContextFactory.UseAsync(machine, machine.CommandReceived, state,
            new MembershipCommand(futureId, firstId, secondId),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), commands,
            TestContext.Current.CancellationToken,
            responseAddress: new Uri("loopback://localhost/membership-result"), requestId: Guid.NewGuid());
        Assert.Equal(2, commands.Messages.Count);
        Assert.Equal(2, state.Pending.Count);
        Assert.Contains(firstId, state.Pending);
        Assert.Contains(secondId, state.Pending);

        var responses = new OutgoingMessageRecorder();
        Task RaiseAsync(Guid id, string value) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.ResponseReceived, state, new MembershipResponse(id, value),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context), responses,
            TestContext.Current.CancellationToken, requestId: futureId);
        Task VerifyStoredResponseAsync(Guid id, string value) => FutureBehaviorContextFactory.UseAsync(
            machine, machine.ResponseReceived, state, new MembershipResponse(id, "read-only-inspection"),
            context =>
            {
                Assert.True(context.TryGetResult<MembershipResponse>(id, out var actual));
                Assert.Equal(new MembershipResponse(id, value), actual);
                return Task.CompletedTask;
            }, cancellationToken: TestContext.Current.CancellationToken);
        Task VerifyFinalAsync() => FutureBehaviorContextFactory.UseAsync(
            machine, machine.ResponseReceived, state, new MembershipResponse(firstId, "read-only-inspection"),
            context =>
            {
                Assert.True(context.TryGetResult<ResultMessage>(futureId, out var actual));
                Assert.Equal(new ResultMessage("first,second"), actual);
                return Task.CompletedTask;
            }, cancellationToken: TestContext.Current.CancellationToken);

        if (replayFirst)
        {
            await RaiseAsync(firstId, "first");
            await RaiseAsync(firstId, "forged-replay");
            await VerifyStoredResponseAsync(firstId, "first");
            Assert.Single(state.Results);
            Assert.Equal(secondId, Assert.Single(state.Pending));
        }
        else
        {
            Guid unknown = Guid.Parse("018cc251-f400-7000-8000-000000000504");
            await RaiseAsync(unknown, "unknown-result");
            Assert.Empty(state.Results);
            Assert.Equal(2, state.Pending.Count);
            Assert.Contains(firstId, state.Pending);
            Assert.Contains(secondId, state.Pending);
            await RaiseAsync(firstId, "first");
        }
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Equal(0, machine.FinalFactoryCalls);
        await RaiseAsync(secondId, "second");
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Equal(1, machine.FinalFactoryCalls);
        await VerifyFinalAsync();
        int terminalSends = responses.Messages.Count;
        Assert.Equal(1, terminalSends);

        await RaiseAsync(firstId, "forged-terminal-replay");
        await VerifyStoredResponseAsync(firstId, "first");
        await VerifyStoredResponseAsync(secondId, "second");
        await VerifyFinalAsync();
        Assert.Equal(1, machine.FinalFactoryCalls);
        Assert.Equal(terminalSends, responses.Messages.Count);
    }

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
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
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
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
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
            Factory = new ContextMessageFactory<IBehaviorContext<FutureState, Signal>, FaultMessage>(_ =>
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

    private static ContextMessageFactory<IBehaviorContext<FutureState, Signal>, ResultMessage> CreateResultFactory()
    {
        return new ContextMessageFactory<IBehaviorContext<FutureState, Signal>, ResultMessage>(
            context => Task.FromResult(new InitializedMessage<ResultMessage>(new ResultMessage(context.Message.Value))));
    }

    private static ContextMessageFactory<IBehaviorContext<FutureState, Signal>, FaultMessage> CreateFaultFactory()
    {
        return new ContextMessageFactory<IBehaviorContext<FutureState, Signal>, FaultMessage>(
            context => Task.FromResult(new InitializedMessage<FaultMessage>(new FaultMessage(context.Message.Value))));
    }

    private sealed class MembershipFuture : Future<MembershipCommand, ResultMessage>
    {
        public int FinalFactoryCalls { get; private set; }
        public IEvent<MembershipResponse> ResponseReceived { get; }
        public MembershipFuture()
        {
            ConfigureCommand(configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            ResponseReceived = SendRequests<MembershipRequest, MembershipRequest>(
                    command => new[] { new MembershipRequest(command.FirstId), new MembershipRequest(command.SecondId) },
                    configuration =>
                    {
                        configuration.SetRequestFactory(context => context.Message);
                        configuration.TrackPendingRequest(request => request.RequestId);
                    })
                .OnResponseReceived<MembershipResponse>(configuration =>
                    configuration.CompletePendingRequest(response => response.RequestId)).Completed;
            WhenAllCompleted(configuration => configuration.SetResultFactory(context =>
            {
                FinalFactoryCalls++;
                return new ResultMessage(string.Join(",", context.SelectResults<MembershipResponse>()
                    .Select(response => response.Value).Order(StringComparer.Ordinal)));
            }));
        }
    }

    public sealed record MembershipCommand(Guid CorrelationId, Guid FirstId, Guid SecondId) : ICorrelatedBy<Guid>;
    public sealed record MembershipRequest(Guid RequestId);
    public sealed record MembershipResponse(Guid RequestId, string Value);

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<Signal> SignalReceived { get; private set; } = null!;
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

        public IEvent<ProbeResponse> ResponseReceived { get; }
    }

    public sealed record Signal(string Value);

    public sealed record ProbeCommand(Guid CorrelationId, Guid RequestId) : ICorrelatedBy<Guid>;

    public sealed record ProbeRequest(Guid RequestId);

    public sealed record ProbeResponse(Guid RequestId);

    public sealed record Command(string Value);

    public sealed record ResultMessage(string Value);

    public sealed record FaultMessage(string Value);

    private sealed class ExpectedDispatchException : Exception;
}
