using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.SagaStateMachine;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Components;

public sealed class RequestStateMachineDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "state-defaults-and-persistence-contract")]
    public void RequestState_StartsUnassignedAndExposesEveryPersistedOwnerField()
    {
        var state = new RequestState();

        Assert.Equal(Guid.Empty, state.CorrelationId);
        Assert.Equal(Guid.Empty, state.SagaCorrelationId);
        Assert.Equal(0, state.CurrentState);
        Assert.Equal(0, state.Version);
        Assert.Null(state.ConversationId);
        Assert.Null(state.ExpirationTime);
        Assert.Null(state.ResponseAddress);
        Assert.Null(state.FaultAddress);
        Assert.Null(state.SagaAddress);
        Assert.IsAssignableFrom<ISagaStateMachineInstance>(state);
        Assert.IsAssignableFrom<ISagaVersion>(state);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "owned-events-states-and-correlation-strategies")]
    public void MachineInstances_OwnDistinctDescriptorsAndTheExactCorrelationStrategies()
    {
        var first = new RequestStateMachine();
        var second = new RequestStateMachine();

        Assert.NotSame(first.Pending, second.Pending);
        Assert.NotSame(first.Started, second.Started);
        Assert.NotSame(first.Completed, second.Completed);
        Assert.NotSame(first.Faulted, second.Faulted);
        Assert.Equal(
            [first.Final, first.Initial, first.Pending],
            first.States.OrderBy(static state => state.Name, StringComparer.Ordinal));
        Assert.Equal(
            [first.Completed, first.Faulted, first.Started],
            first.Events.OrderBy(static @event => @event.Name, StringComparer.Ordinal));

        IEventCorrelation<RequestState, IRequestStarted> started = Assert.Single(
            first.Correlations.OfType<IEventCorrelation<RequestState, IRequestStarted>>());
        Assert.Same(first.Started, started.Event);
        Assert.NotNull(started.MessageFilter);
        Assert.NotNull(started.FilterFactory);
        Assert.Equal("NewOrExistingSagaPolicy`2", started.Policy!.GetType().Name);

        IEventCorrelation<RequestState, IRequestCompleted> completed = Assert.Single(
            first.Correlations.OfType<IEventCorrelation<RequestState, IRequestCompleted>>());
        Assert.Same(first.Completed, completed.Event);
        Assert.NotNull(completed.MessageFilter);
        Assert.NotNull(completed.FilterFactory);
        Assert.Equal("AnyExistingSagaPolicy`2", completed.Policy!.GetType().Name);

        IEventCorrelation<RequestState, IRequestFaulted> faulted = Assert.Single(
            first.Correlations.OfType<IEventCorrelation<RequestState, IRequestFaulted>>());
        Assert.Same(first.Faulted, faulted.Event);
        Assert.NotNull(faulted.MessageFilter);
        Assert.NotNull(faulted.FilterFactory);
        Assert.Equal("AnyExistingSagaPolicy`2", faulted.Policy!.GetType().Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "started-initialization-and-source-boundary")]
    public async Task Started_CopiesTheCompleteRouteAndRejectsAMissingSagaSourceAsync()
    {
        var machine = new RequestStateMachine();
        Guid requestId = NewId.NextGuid();
        Guid sagaCorrelationId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        Uri responseAddress = new("loopback://localhost/original-response");
        Uri faultAddress = new("loopback://localhost/original-fault");
        Uri sagaAddress = new("loopback://localhost/original-saga");
        DateTimeOffset expirationTime = DateTimeOffset.UtcNow.AddMinutes(2);
        var message = new StartedMessage(
            sagaCorrelationId,
            requestId,
            responseAddress,
            faultAddress,
            expirationTime,
            ["urn:message:test-request"],
            new object());
        var state = new RequestState { CorrelationId = requestId };

        await RaiseAsync(
            machine,
            state,
            machine.Started,
            message,
            sagaAddress,
            conversationId,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(requestId, state.CorrelationId);
        Assert.Equal(sagaCorrelationId, state.SagaCorrelationId);
        Assert.Equal(conversationId, state.ConversationId);
        Assert.Equal(responseAddress, state.ResponseAddress);
        Assert.Equal(faultAddress, state.FaultAddress);
        Assert.Equal(sagaAddress, state.SagaAddress);
        Assert.Equal(expirationTime, state.ExpirationTime);
        Assert.Same(machine.Pending, await StateMachineTestExecution.GetStateAsync(machine, state));

        var missingSourceState = new RequestState { CorrelationId = requestId };
        EventExecutionException exception = await Assert.ThrowsAsync<EventExecutionException>(() =>
            RaiseAsync(
                machine,
                missingSourceState,
                machine.Started,
                message,
                null,
                conversationId,
                cancellationToken: TestContext.Current.CancellationToken));

        InvalidOperationException ownerFailure = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("A source address is required when a saga request is started.", ownerFailure.Message);
        Assert.Same(machine.Initial, await StateMachineTestExecution.GetStateAsync(machine, missingSourceState));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "terminal-outcomes-finalize-and-expired-outcomes-do-not-send")]
    public async Task TerminalOutcome_FinalizesExactlyOnceWithoutSendingAnExpiredOutcomeAsync(bool faulted)
    {
        var machine = new RequestStateMachine();
        Guid requestId = NewId.NextGuid();
        Guid sagaCorrelationId = NewId.NextGuid();
        var state = new RequestState { CorrelationId = requestId };
        var recorder = new OutgoingMessageRecorder();
        var started = new StartedMessage(
            sagaCorrelationId,
            requestId,
            new Uri("loopback://localhost/response"),
            new Uri("loopback://localhost/fault"),
            DateTimeOffset.MinValue,
            ["urn:message:test-request"],
            new object());

        await RaiseAsync(
            machine,
            state,
            machine.Started,
            started,
            new Uri("loopback://localhost/saga"),
            NewId.NextGuid(),
            recorder,
            TestContext.Current.CancellationToken);

        if (faulted)
        {
            await RaiseAsync(
                machine,
                state,
                machine.Faulted,
                new FaultedMessage(sagaCorrelationId, ["urn:message:test-fault"], new object()),
                outgoingMessages: recorder,
                cancellationToken: TestContext.Current.CancellationToken);
        }
        else
        {
            await RaiseAsync(
                machine,
                state,
                machine.Completed,
                new CompletedMessage(sagaCorrelationId, DateTimeOffset.UtcNow, ["urn:message:test-response"], new object()),
                outgoingMessages: recorder,
                cancellationToken: TestContext.Current.CancellationToken);
        }

        Assert.Empty(recorder.Messages);
        Assert.Same(machine.Final, await StateMachineTestExecution.GetStateAsync(machine, state));
        Assert.True(await StateMachineTestExecution.IsCompletedAsync(
            machine,
            state,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-STATE-MACHINE", "pre-cancellation-has-no-state-effects")]
    public async Task Started_PreCanceledOperationLeavesEveryStateFieldUntouchedAsync()
    {
        var machine = new RequestStateMachine();
        Guid requestId = NewId.NextGuid();
        var state = new RequestState { CorrelationId = requestId };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RaiseAsync(
                machine,
                state,
                machine.Started,
                new StartedMessage(
                    NewId.NextGuid(),
                    requestId,
                    new Uri("loopback://localhost/response"),
                    new Uri("loopback://localhost/fault"),
                    null,
                    ["urn:message:test-request"],
                    new object()),
                new Uri("loopback://localhost/saga"),
                NewId.NextGuid(),
                cancellationToken: cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(requestId, state.CorrelationId);
        Assert.Equal(Guid.Empty, state.SagaCorrelationId);
        Assert.Null(state.ConversationId);
        Assert.Null(state.ResponseAddress);
        Assert.Null(state.FaultAddress);
        Assert.Null(state.SagaAddress);
        Assert.Null(state.ExpirationTime);
        Assert.Same(machine.Initial, await StateMachineTestExecution.GetStateAsync(machine, state));
    }

    private static async Task RaiseAsync<TMessage>(
        RequestStateMachine machine,
        RequestState state,
        IEvent<TMessage> @event,
        TMessage message,
        Uri? sourceAddress = null,
        Guid? conversationId = null,
        OutgoingMessageRecorder? outgoingMessages = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ConsumeContext<TMessage> inner = InMemoryOutboxTestContextFactory.Create(
            message,
            cancellationToken,
            outgoingMessages: outgoingMessages,
            sourceAddress: sourceAddress);
        ConsumeContext<TMessage> consumeContext = new MetadataConsumeContext<TMessage>(inner, conversationId);
        var sagaInstance = new SagaInstance<RequestState>(state);
        await sagaInstance.MarkInUseAsync(TestContext.Current.CancellationToken);
        using var sagaContext = new InMemorySagaConsumeContext<RequestState, TMessage>(consumeContext, sagaInstance);
        IBehaviorContext<RequestState, TMessage> behaviorContext =
            new ViciOneServiceBusStateMachine<RequestState>.BehaviorContextProxy<TMessage>(
                machine,
                sagaContext,
                sagaContext,
                @event);

        await ((IStateMachine<RequestState>)machine).RaiseEventAsync(behaviorContext, cancellationToken);
    }

    private sealed class MetadataConsumeContext<TMessage>(ConsumeContext<TMessage> context, Guid? conversationId) :
        ConsumeContextProxy<TMessage>(context)
        where TMessage : class
    {
        public override Guid? ConversationId { get; } = conversationId;
    }

    private sealed record StartedMessage(
        Guid CorrelationId,
        Guid RequestId,
        Uri ResponseAddress,
        Uri FaultAddress,
        DateTimeOffset? ExpirationTime,
        string[] PayloadType,
        object Payload) : IRequestStarted;

    private sealed record CompletedMessage(
        Guid CorrelationId,
        DateTimeOffset Timestamp,
        string[] PayloadType,
        object Payload) : IRequestCompleted;

    private sealed record FaultedMessage(
        Guid CorrelationId,
        string[] PayloadType,
        object Payload) : IRequestFaulted;
}
