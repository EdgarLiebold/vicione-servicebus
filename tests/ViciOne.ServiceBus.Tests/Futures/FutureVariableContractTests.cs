using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureVariableContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "all-binder-overloads-store-values-when-their-events-run")]
    public async Task BinderExtensions_ExecuteAllTypedAndStateFactoryShapesAsync()
    {
        var machine = new VariableBinderMachine();
        var state = new FutureState();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                await ((IStateMachine<FutureState>)machine).RaiseEventAsync(context);
                await context.RaiseAsync(machine.StateSignal, context.CancellationToken);
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("input-typed-sync", Assert.IsType<StoredValue>(state.Variables["typed-sync"]).Value);
        Assert.Equal("input-typed-async", Assert.IsType<StoredValue>(state.Variables["typed-async"]).Value);
        Assert.Equal("state-sync", Assert.IsType<StoredValue>(state.Variables["state-sync"]).Value);
        Assert.Equal("state-async", Assert.IsType<StoredValue>(state.Variables["state-async"]).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "binder-and-factory-boundaries-are-required")]
    public void BinderExtensions_RejectMissingBindersFactoriesAndKeys()
    {
        EventMessageFactory<FutureState, Signal, StoredValue> typedSync = _ => new StoredValue("value");
        AsyncEventMessageFactory<FutureState, Signal, StoredValue> typedAsync = _ => Task.FromResult(new StoredValue("value"));
        EventMessageFactory<FutureState, StoredValue> stateSync = _ => new StoredValue("value");
        AsyncEventMessageFactory<FutureState, StoredValue> stateAsync = _ => Task.FromResult(new StoredValue("value"));

        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureVariableExtensions.SetVariable<Signal, StoredValue>(
                (IEventActivityBinder<FutureState, Signal>)null!, "value", typedSync)).ParamName);
        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureVariableExtensions.SetVariableAwaited<Signal, StoredValue>(
                (IEventActivityBinder<FutureState, Signal>)null!, "value", typedAsync)).ParamName);
        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureVariableExtensions.SetVariable<StoredValue>(
                (IEventActivityBinder<FutureState>)null!, "value", stateSync)).ParamName);
        Assert.Equal("binder", Assert.Throws<ArgumentNullException>(() =>
            FutureVariableExtensions.SetVariableAwaited<StoredValue>(
                (IEventActivityBinder<FutureState>)null!, "value", stateAsync)).ParamName);

        var machine = new BinderBoundaryMachine();
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => machine.AddTypedSync(" ", typedSync)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => machine.AddTypedAwaited(" ", typedAsync)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => machine.AddStateSync(" ", stateSync)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentException>(() => machine.AddStateAwaited(" ", stateAsync)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => machine.AddTypedSync("value", null!)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => machine.AddTypedAwaited("value", null!)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => machine.AddStateSync("value", null!)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => machine.AddStateAwaited("value", null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "typed-and-state-factories-store-and-replace-values")]
    public async Task SynchronousFactories_StoreTypedAndStateValuesAndReplaceCaseInsensitivelyAsync()
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
                StoredValue typed = context.SetVariable("Result", current => new StoredValue(current.Message.Value));
                IBehaviorContext<FutureState> stateContext = context;
                StoredValue replaced = stateContext.SetVariable("result", _ => new StoredValue("replacement"));
                stateContext.SetVariable("Direct", new StoredValue("direct"));

                Assert.Equal("input", typed.Value);
                Assert.Equal("replacement", replaced.Value);
                Assert.Equal(2, state.Variables.Count);
                Assert.Same(replaced, state.Variables["RESULT"]);
                Assert.True(stateContext.TryGetVariable("direct", out StoredValue? direct));
                Assert.Equal("direct", direct.Value);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "typed-and-state-async-factories-store-returned-values")]
    public async Task AsynchronousFactories_StoreAndReturnTheirExactValuesAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState();
        using var source = new CancellationTokenSource();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                StoredValue typed = await context.SetVariableAsync(
                    "typed",
                    current => Task.FromResult(new StoredValue(current.Message.Value)),
                    source.Token);
                IBehaviorContext<FutureState> stateContext = context;
                StoredValue untyped = await stateContext.SetVariableAsync(
                    "state",
                    _ => Task.FromResult(new StoredValue("state")),
                    source.Token);

                Assert.Same(typed, state.Variables["typed"]);
                Assert.Same(untyped, state.Variables["state"]);
            },
            cancellationToken: source.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "cancellation-precedes-async-factory")]
    public async Task AsynchronousFactory_ObservesPreCancellationBeforeInvokingUserCodeAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState();
        using var source = new CancellationTokenSource();
        source.Cancel();
        bool invoked = false;

        await Assert.ThrowsAsync<OperationCanceledException>(() => FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context => context.SetVariableAsync(
                "value",
                _ =>
                {
                    invoked = true;
                    return Task.FromResult(new StoredValue("unexpected"));
                },
                source.Token),
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.False(invoked);
        Assert.Empty(state.Variables);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "variable-name-is-nonempty")]
    public async Task VariableOperations_RejectInvalidNamesWithoutChangingStateAsync(string key)
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
                Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
                    stateContext.SetVariable(key, new StoredValue("value"))).ParamName);
                Assert.Equal("key", Assert.Throws<ArgumentException>(() =>
                    stateContext.TryGetVariable<StoredValue>(key, out _)).ParamName);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(state.Variables);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "factory-result-is-required")]
    public async Task VariableFactories_RejectNullResultsWithoutChangingStateAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState();

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            async context =>
            {
                Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
                    context.SetVariable<Signal, StoredValue>("sync", _ => null!)).ParamName);
                ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                    context.SetVariableAsync<Signal, StoredValue>("async", _ => Task.FromResult<StoredValue>(null!),
                        context.CancellationToken));
                Assert.Equal("value", exception.ParamName);
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(state.Variables);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-VARIABLES", "missing-or-incompatible-value-is-not-returned")]
    public async Task TryGetVariable_ReturnsFalseForMissingOrIncompatibleValuesAsync()
    {
        var machine = new ContextMachine();
        var state = new FutureState { Variables = new Dictionary<string, object> { ["value"] = 42 } };

        await FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            context =>
            {
                IBehaviorContext<FutureState> stateContext = context;
                Assert.False(stateContext.TryGetVariable("missing", out StoredValue? missing));
                Assert.Null(missing);
                Assert.False(stateContext.TryGetVariable("value", out StoredValue? incompatible));
                Assert.Null(incompatible);
                return Task.CompletedTask;
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<Signal> SignalReceived { get; private set; } = null!;
    }

    private sealed class VariableBinderMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public VariableBinderMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(SignalReceived)
                    .SetVariable("typed-sync", context => new StoredValue(context.Message.Value + "-typed-sync"))
                    .SetVariableAwaited("typed-async", async context =>
                        {
                            await Task.Yield();
                            return new StoredValue(context.Message.Value + "-typed-async");
                        }),
                When(StateSignal)
                    .SetVariable("state-sync", _ => new StoredValue("state-sync"))
                    .SetVariableAwaited("state-async", async _ =>
                        {
                            await Task.Yield();
                            return new StoredValue("state-async");
                        }));
        }

        public IEvent<Signal> SignalReceived { get; private set; } = null!;

        public IEvent StateSignal { get; private set; } = null!;
    }

    private sealed class BinderBoundaryMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public BinderBoundaryMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public IEvent<Signal> TypedEvent { get; private set; } = null!;

        public IEvent StateEvent { get; private set; } = null!;

        public void AddTypedSync(string key, EventMessageFactory<FutureState, Signal, StoredValue> factory) =>
            When(TypedEvent).SetVariable(key, factory);

        public void AddTypedAwaited(string key, AsyncEventMessageFactory<FutureState, Signal, StoredValue> factory) =>
            When(TypedEvent).SetVariableAwaited(key, factory);

        public void AddStateSync(string key, EventMessageFactory<FutureState, StoredValue> factory) =>
            When(StateEvent).SetVariable(key, factory);

        public void AddStateAwaited(string key, AsyncEventMessageFactory<FutureState, StoredValue> factory) =>
            When(StateEvent).SetVariableAwaited(key, factory);
    }

    public sealed record Signal(string Value);

    public sealed record StoredValue(string Value);
}
