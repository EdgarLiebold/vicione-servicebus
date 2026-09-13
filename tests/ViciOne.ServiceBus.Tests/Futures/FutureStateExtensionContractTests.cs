using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureStateExtensionContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "binder-result-factories-run-synchronously-and-asynchronously")]
    public async Task ResultBinderExtensions_StoreSynchronousAndAwaitedFactoryValuesAsync()
    {
        Guid synchronousId = Guid.NewGuid();
        Guid awaitedId = Guid.NewGuid();
        var machine = new ResultBinderMachine(synchronousId, awaitedId);
        var state = new FutureState { Pending = [synchronousId, awaitedId] };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SynchronousReceived,
            state,
            new Signal("sync"),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
            cancellationToken: TestContext.Current.CancellationToken);
        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.AwaitedReceived,
            state,
            new Signal("async"),
            context => ((IStateMachine<FutureState>)machine).RaiseEventAsync(context),
            cancellationToken: TestContext.Current.CancellationToken);

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SynchronousReceived,
            state,
            new Signal("inspect"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                Assert.True(stateContext.TryGetResult(synchronousId, out StoredPayload? synchronous));
                Assert.True(stateContext.TryGetResult(awaitedId, out StoredPayload? awaited));
                Assert.Equal("sync-result", synchronous.Value);
                Assert.Equal("async-result", awaited.Value);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(state.Pending);
        Assert.NotNull(state.Completed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "binder-result-dependencies-are-required")]
    public void ResultBinderExtensions_RejectMissingBinderSelectorsAndFactories()
    {
        Func<IBehaviorContext<FutureState, Signal>, Guid> idProvider = _ => Guid.NewGuid();
        EventMessageFactory<FutureState, Signal, StoredPayload> synchronous = _ => new StoredPayload("value");
        AsyncEventMessageFactory<FutureState, Signal, StoredPayload> awaited = _ => Task.FromResult(new StoredPayload("value"));

        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureExtensions.SetResult<Signal, StoredPayload>(null!, idProvider, synchronous)).ParamName);
        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureExtensions.SetResultAwaited<Signal, StoredPayload>(null!, idProvider, awaited)).ParamName);

        var machine = new ResultBinderBoundaryMachine();
        Assert.Equal("getResultId", Assert.Throws<ArgumentNullException>(() => machine.AddSynchronous(null!, synchronous)).ParamName);
        Assert.Equal("messageFactory", Assert.Throws<ArgumentNullException>(() => machine.AddSynchronous(idProvider, null!)).ParamName);
        Assert.Equal("getResultId", Assert.Throws<ArgumentNullException>(() => machine.AddAwaited(null!, awaited)).ParamName);
        Assert.Equal("messageFactory", Assert.Throws<ArgumentNullException>(() => machine.AddAwaited(idProvider, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "stored-messages-round-trip-through-context-serializer")]
    public async Task StoredMessage_RoundTripsOnlyThroughItsDeclaredContractsAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                FutureMessage stored = stateContext.CreateFutureMessage(new StoredPayload("stored"));
                state.Command = stored;

                StoredPayload command = Assert.IsType<StoredPayload>(stateContext.GetCommand<StoredPayload>());
                StoredPayload projected = Assert.IsType<StoredPayload>(stateContext.ToObject<StoredPayload>(stored));
                Assert.Equal("stored", command.Value);
                Assert.Equal("stored", projected.Value);
                Assert.Null(stateContext.GetCommand<OtherPayload>());
                Assert.Null(stateContext.ToObject<OtherPayload>(stored));
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LIFECYCLE", "completion-waits-for-all-pending-work-and-no-fault")]
    public async Task SetCompleted_UsesTheMessageTimestampOnlyAfterTheLastSuccessfulPendingOperationAsync()
    {
        Guid first = Guid.Parse("72d1c00d-922c-41ee-9c80-c4de22420223");
        Guid second = Guid.Parse("d1d39fd9-1661-49bb-a291-f676c4c0fb54");
        var machine = new ContextMachine();
        var state = new FutureState { Pending = [first, second] };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                stateContext.SetCompleted(first);
                Assert.Null(state.Completed);
                Assert.Equal([second], state.Pending);

                stateContext.SetCompleted(second);
                Assert.Equal(DateTimeOffset.UnixEpoch, state.Completed);
                Assert.Empty(state.Pending);

                state.Completed = null;
                state.Faults[Guid.NewGuid()] = new FutureMessage();
                stateContext.SetCompleted(Guid.NewGuid());
                Assert.Null(state.Completed);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LIFECYCLE", "fault-preserves-first-timestamp-and-removes-pending-work")]
    public async Task SetFaulted_PreservesTheFirstFaultTimestampAndCompletesEachPendingIdentifierAsync()
    {
        Guid first = Guid.Parse("657fbd3a-a31e-481b-b8e6-251587cd64fa");
        Guid second = Guid.Parse("ff32402d-e8b3-4813-91e0-27a018e82577");
        DateTimeOffset firstFault = new(2026, 4, 2, 3, 4, 5, TimeSpan.Zero);
        DateTimeOffset laterFault = firstFault.AddMinutes(1);
        var machine = new ContextMachine();
        var state = new FutureState { Pending = [first, second] };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                stateContext.SetFaulted(first, firstFault);
                stateContext.SetFaulted(second, laterFault);

                Assert.Equal(firstFault, state.Faulted);
                Assert.Empty(state.Pending);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-RESULTS", "all-result-overloads-store-and-return-their-values")]
    public async Task ResultOperations_StoreEveryFactoryShapeAndExposeTypedLookupAsync()
    {
        Guid[] ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var machine = new ContextMachine();
        var state = new FutureState { Pending = ids.ToHashSet() };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                StoredPayload typedSync = context.SetResult<Signal, StoredPayload>(ids[0],
                    current => new StoredPayload(current.Message.Value + "-typed-sync"));
                IBehaviorContext<FutureState> stateContext = context;
                StoredPayload stateSync = stateContext.SetResult(ids[1], _ => new StoredPayload("state-sync"));
                var direct = new StoredPayload("direct");
                stateContext.SetResult(ids[2], direct);
                StoredPayload typedAsync = await context.SetResultAsync<Signal, StoredPayload>(ids[3],
                    current => Task.FromResult(new StoredPayload(current.Message.Value + "-typed-async")),
                    context.CancellationToken);
                StoredPayload stateAsync = await stateContext.SetResultAsync(ids[4],
                    _ => Task.FromResult(new StoredPayload("state-async")),
                    context.CancellationToken);

                Assert.Equal("input-typed-sync", typedSync.Value);
                Assert.Equal("state-sync", stateSync.Value);
                Assert.Equal("input-typed-async", typedAsync.Value);
                Assert.Equal("state-async", stateAsync.Value);
                Assert.Equal(DateTimeOffset.UnixEpoch, state.Completed);
                Assert.Empty(state.Pending);
                Assert.Equal(5, state.Results.Count);
                Assert.True(stateContext.TryGetResult(ids[3], out StoredPayload? result));
                Assert.Equal("input-typed-async", result.Value);
                Assert.True(stateContext.TryGetResult(ids[2], out StoredPayload? directResult));
                Assert.Equal(direct.Value, directResult.Value);
                Assert.False(stateContext.TryGetResult(Guid.NewGuid(), out StoredPayload? missing));
                Assert.Null(missing);
                Assert.Equal(5, stateContext.SelectResults<StoredPayload>().Count());
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-FAULTS", "all-fault-overloads-store-and-return-their-values")]
    public async Task FaultOperations_StoreEveryFactoryShapeAndExposeTypedLookupAsync()
    {
        Guid[] ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var machine = new ContextMachine();
        var state = new FutureState { Pending = ids.ToHashSet() };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                var direct = new StoredFault("direct");
                stateContext.SetFault(ids[0], direct);
                StoredFault synchronous = context.SetFault<Signal, StoredFault>(ids[1],
                    current => new StoredFault(current.Message.Value + "-sync"));
                StoredFault asynchronous = await context.SetFaultAsync<Signal, StoredFault>(ids[2],
                    current => Task.FromResult(new StoredFault(current.Message.Value + "-async")),
                    context.CancellationToken);

                Assert.Equal("input-sync", synchronous.Value);
                Assert.Equal("input-async", asynchronous.Value);
                Assert.Equal(DateTimeOffset.UnixEpoch, state.Faulted);
                Assert.Empty(state.Pending);
                Assert.Equal(3, state.Faults.Count);
                Assert.True(stateContext.TryGetFault(ids[0], out StoredFault? fault));
                Assert.Equal("direct", fault.Value);
                Assert.False(stateContext.TryGetFault(Guid.NewGuid(), out StoredFault? missing));
                Assert.Null(missing);
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-LIFECYCLE", "factory-failure-does-not-complete-pending-work")]
    public async Task ResultAndFaultFactories_DoNotMutateLifecycleStateWhenCreationFailsAsync()
    {
        Guid resultId = Guid.NewGuid();
        Guid faultId = Guid.NewGuid();
        var machine = new ContextMachine();
        var state = new FutureState { Pending = [resultId, faultId] };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                Assert.Equal("result", Assert.Throws<ArgumentNullException>(() =>
                    context.SetResult<Signal, StoredPayload>(resultId, _ => null!)).ParamName);
                ArgumentNullException faultException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                    context.SetFaultAsync<Signal, StoredFault>(faultId, _ => Task.FromResult<StoredFault>(null!),
                        context.CancellationToken));
                Assert.Equal("fault", faultException.ParamName);
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { resultId, faultId }.OrderBy(value => value), state.Pending.OrderBy(value => value));
        Assert.Null(state.Completed);
        Assert.Null(state.Faulted);
        Assert.Empty(state.Results);
        Assert.Empty(state.Faults);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-SUBSCRIPTIONS", "response-address-and-request-id-are-deduplicated")]
    public async Task AddSubscription_AddsOneExactSubscriberAndIgnoresContextsWithoutAResponseAddressAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState();
        var responseAddress = new Uri("loopback://localhost/future-subscriber");
        Guid requestId = Guid.NewGuid();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("with-address"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                stateContext.AddSubscription();
                stateContext.AddSubscription();
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken,
            responseAddress: responseAddress,
            requestId: requestId);
        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("without-address"),
            context =>
            {
                ((IBehaviorContext<FutureState>)context).AddSubscription();
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);

        FutureSubscription subscription = Assert.Single(state.Subscriptions);
        Assert.Equal(responseAddress, subscription.Address);
        Assert.Equal(requestId, subscription.RequestId);
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<Signal> SignalReceived { get; private set; } = null!;
    }

    private sealed class ResultBinderMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ResultBinderMachine(Guid synchronousId, Guid awaitedId)
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(SynchronousReceived)
                    .SetResult(_ => synchronousId, context => new StoredPayload(context.Message.Value + "-result")),
                When(AwaitedReceived)
                    .SetResultAwaited(_ => awaitedId, async context =>
                    {
                        await Task.Yield();
                        return new StoredPayload(context.Message.Value + "-result");
                    }));
        }

        public IEvent<Signal> SynchronousReceived { get; private set; } = null!;

        public IEvent<Signal> AwaitedReceived { get; private set; } = null!;
    }

    private sealed class ResultBinderBoundaryMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ResultBinderBoundaryMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<Signal> SignalReceived { get; private set; } = null!;

        public void AddSynchronous(Func<IBehaviorContext<FutureState, Signal>, Guid> idProvider,
            EventMessageFactory<FutureState, Signal, StoredPayload> factory) =>
            When(SignalReceived).SetResult(idProvider, factory);

        public void AddAwaited(Func<IBehaviorContext<FutureState, Signal>, Guid> idProvider,
            AsyncEventMessageFactory<FutureState, Signal, StoredPayload> factory) =>
            When(SignalReceived).SetResultAwaited(idProvider, factory);
    }

    public sealed record Signal(string Value);

    public sealed record StoredPayload(string Value);

    public sealed record OtherPayload(string Value);

    public sealed record StoredFault(string Value);
}
