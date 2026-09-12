using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureTerminalConfiguratorContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "event-result-factory-overloads-produce-and-store-results")]
    public async Task EventResultConfigurator_ExecutesSynchronousAndAsynchronousFactoriesAsync()
    {
        TerminalMessage synchronous = await ExecuteEventResultAsync(configurator =>
            configurator.SetResultFactory(context => new TerminalMessage(context.Message.Value + "-sync")));
        TerminalMessage asynchronous = await ExecuteEventResultAsync(configurator =>
            configurator.SetResultFactory(async context =>
            {
                await Task.Yield();
                return new TerminalMessage(context.Message.Value + "-async");
            }));

        Assert.Equal("input-sync", synchronous.Value);
        Assert.Equal("input-async", asynchronous.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "event-result-initializer-produces-and-stores-result")]
    public async Task EventResultConfigurator_ExecutesInitializerAsync()
    {
        TerminalMessage result = await ExecuteEventResultAsync(configurator =>
            configurator.SetResultInitializer(context => new { Value = context.Message.Value + "-initializer" }));

        Assert.Equal("input-initializer", result.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "state-result-factory-overloads-produce-and-store-results")]
    public async Task StateResultConfigurator_ExecutesSynchronousAndAsynchronousFactoriesAsync()
    {
        TerminalMessage synchronous = await ExecuteStateResultAsync(configurator =>
            configurator.SetResultFactory(_ => new TerminalMessage("state-sync")));
        TerminalMessage asynchronous = await ExecuteStateResultAsync(configurator =>
            configurator.SetResultFactory(async _ =>
            {
                await Task.Yield();
                return new TerminalMessage("state-async");
            }));

        Assert.Equal("state-sync", synchronous.Value);
        Assert.Equal("state-async", asynchronous.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "state-result-initializer-produces-and-stores-result")]
    public async Task StateResultConfigurator_ExecutesInitializerAsync()
    {
        TerminalMessage result = await ExecuteStateResultAsync(configurator =>
            configurator.SetResultInitializer(_ => new { Value = "state-initializer" }));

        Assert.Equal("state-initializer", result.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "event-fault-factory-overloads-produce-and-store-faults")]
    public async Task EventFaultConfigurator_ExecutesSynchronousAndAsynchronousFactoriesAsync()
    {
        TerminalMessage synchronous = await ExecuteEventFaultAsync(configurator =>
            configurator.SetFaultFactory(context => new TerminalMessage(context.Message.Value + "-sync")));
        TerminalMessage asynchronous = await ExecuteEventFaultAsync(configurator =>
            configurator.SetFaultFactory(async context =>
            {
                await Task.Yield();
                return new TerminalMessage(context.Message.Value + "-async");
            }));

        Assert.Equal("input-sync", synchronous.Value);
        Assert.Equal("input-async", asynchronous.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "event-fault-initializer-produces-and-stores-fault")]
    public async Task EventFaultConfigurator_ExecutesInitializerAsync()
    {
        TerminalMessage fault = await ExecuteEventFaultAsync(configurator =>
            configurator.SetFaultInitializer(context => new { Value = context.Message.Value + "-initializer" }));

        Assert.Equal("input-initializer", fault.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "state-fault-factory-overloads-produce-and-store-faults")]
    public async Task StateFaultConfigurator_ExecutesSynchronousAndAsynchronousFactoriesAsync()
    {
        TerminalMessage synchronous = await ExecuteStateFaultAsync(configurator =>
            configurator.SetFaultFactory(_ => new TerminalMessage("state-sync")));
        TerminalMessage asynchronous = await ExecuteStateFaultAsync(configurator =>
            configurator.SetFaultFactory(async _ =>
            {
                await Task.Yield();
                return new TerminalMessage("state-async");
            }));

        Assert.Equal("state-sync", synchronous.Value);
        Assert.Equal("state-async", asynchronous.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "state-fault-initializer-produces-and-stores-fault")]
    public async Task StateFaultConfigurator_ExecutesInitializerAsync()
    {
        TerminalMessage fault = await ExecuteStateFaultAsync(configurator =>
            configurator.SetFaultInitializer(_ => new { Value = "state-initializer" }));

        Assert.Equal("state-initializer", fault.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-CONFIGURATION", "terminal-configurator-dependencies-and-callbacks-are-required")]
    public void TerminalConfigurators_RejectMissingDependenciesAndCallbacks()
    {
        var eventResult = new FutureResult<Command, TerminalMessage, Signal>();
        var stateResult = new FutureResult<Command, TerminalMessage>();
        var eventFault = new FutureFault<Command, TerminalMessage, Signal>();
        var stateFault = new FutureFault<TerminalMessage>();

        Assert.Equal("result", Assert.Throws<ArgumentNullException>(() =>
            new FutureResultConfigurator<Command, TerminalMessage, Signal>(null!)).ParamName);
        Assert.Equal("result", Assert.Throws<ArgumentNullException>(() =>
            new FutureResultConfigurator<Command, TerminalMessage>(null!)).ParamName);
        Assert.Equal("fault", Assert.Throws<ArgumentNullException>(() =>
            new FutureFaultConfigurator<Command, TerminalMessage, Signal>(null!)).ParamName);
        Assert.Equal("fault", Assert.Throws<ArgumentNullException>(() =>
            new FutureFaultConfigurator<TerminalMessage>(null!)).ParamName);

        var eventResultConfigurator = new FutureResultConfigurator<Command, TerminalMessage, Signal>(eventResult);
        var stateResultConfigurator = new FutureResultConfigurator<Command, TerminalMessage>(stateResult);
        var eventFaultConfigurator = new FutureFaultConfigurator<Command, TerminalMessage, Signal>(eventFault);
        var stateFaultConfigurator = new FutureFaultConfigurator<TerminalMessage>(stateFault);

        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            eventResultConfigurator.SetResultFactory((EventMessageFactory<FutureState, Signal, TerminalMessage>)null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            eventResultConfigurator.SetResultFactory((AsyncEventMessageFactory<FutureState, Signal, TerminalMessage>)null!)).ParamName);
        Assert.Equal("valueProvider", Assert.Throws<ArgumentNullException>(() =>
            eventResultConfigurator.SetResultInitializer(null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            stateResultConfigurator.SetResultFactory((EventMessageFactory<FutureState, TerminalMessage>)null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            stateResultConfigurator.SetResultFactory((AsyncEventMessageFactory<FutureState, TerminalMessage>)null!)).ParamName);
        Assert.Equal("valueProvider", Assert.Throws<ArgumentNullException>(() =>
            stateResultConfigurator.SetResultInitializer(null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            eventFaultConfigurator.SetFaultFactory((EventMessageFactory<FutureState, Signal, TerminalMessage>)null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            eventFaultConfigurator.SetFaultFactory((AsyncEventMessageFactory<FutureState, Signal, TerminalMessage>)null!)).ParamName);
        Assert.Equal("valueProvider", Assert.Throws<ArgumentNullException>(() =>
            eventFaultConfigurator.SetFaultInitializer(null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            stateFaultConfigurator.SetFaultFactory((EventMessageFactory<FutureState, TerminalMessage>)null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            stateFaultConfigurator.SetFaultFactory((AsyncEventMessageFactory<FutureState, TerminalMessage>)null!)).ParamName);
        Assert.Equal("valueProvider", Assert.Throws<ArgumentNullException>(() =>
            stateFaultConfigurator.SetFaultInitializer(null!)).ParamName);
    }

    private static async Task<TerminalMessage> ExecuteEventResultAsync(
        Action<FutureResultConfigurator<Command, TerminalMessage, Signal>> configure)
    {
        var producer = new FutureResult<Command, TerminalMessage, Signal>();
        configure(new FutureResultConfigurator<Command, TerminalMessage, Signal>(producer));
        Assert.Empty(producer.Validate());

        FutureState state = CreatePendingState();
        TerminalMessage? stored = null;
        await UseContextAsync(state, async context =>
        {
            await producer.SetResultAsync(context, context.CancellationToken);
            Assert.True(((BehaviorContext<FutureState>)context).TryGetResult(state.CorrelationId, out stored));
        });

        Assert.NotNull(stored);
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Completed);
        return stored;
    }

    private static async Task<TerminalMessage> ExecuteStateResultAsync(
        Action<FutureResultConfigurator<Command, TerminalMessage>> configure)
    {
        var producer = new FutureResult<Command, TerminalMessage>();
        configure(new FutureResultConfigurator<Command, TerminalMessage>(producer));
        Assert.Empty(producer.Validate());

        FutureState state = CreatePendingState();
        TerminalMessage? stored = null;
        await UseContextAsync(state, async typedContext =>
        {
            BehaviorContext<FutureState> context = typedContext;
            await producer.SetResultAsync(context, context.CancellationToken);
            Assert.True(context.TryGetResult(state.CorrelationId, out stored));
        });

        Assert.NotNull(stored);
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Completed);
        return stored;
    }

    private static async Task<TerminalMessage> ExecuteEventFaultAsync(
        Action<FutureFaultConfigurator<Command, TerminalMessage, Signal>> configure)
    {
        var producer = new FutureFault<Command, TerminalMessage, Signal>();
        configure(new FutureFaultConfigurator<Command, TerminalMessage, Signal>(producer));

        FutureState state = CreatePendingState();
        TerminalMessage? stored = null;
        await UseContextAsync(state, async context =>
        {
            Assert.True(await producer.TrySetFaultedAsync(context, context.CancellationToken));
            Assert.True(((BehaviorContext<FutureState>)context).TryGetFault(state.CorrelationId, out stored));
        });

        Assert.NotNull(stored);
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Faulted);
        return stored;
    }

    private static async Task<TerminalMessage> ExecuteStateFaultAsync(
        Action<FutureFaultConfigurator<TerminalMessage>> configure)
    {
        var producer = new FutureFault<TerminalMessage>();
        configure(new FutureFaultConfigurator<TerminalMessage>(producer));

        FutureState state = CreatePendingState();
        TerminalMessage? stored = null;
        await UseContextAsync(state, async typedContext =>
        {
            BehaviorContext<FutureState> context = typedContext;
            Assert.True(await producer.TrySetFaultedAsync(context, context.CancellationToken));
            Assert.True(context.TryGetFault(state.CorrelationId, out stored));
        });

        Assert.NotNull(stored);
        Assert.Empty(state.Pending);
        Assert.NotNull(state.Faulted);
        return stored;
    }

    private static FutureState CreatePendingState()
    {
        Guid id = Guid.NewGuid();
        return new FutureState
        {
            CorrelationId = id,
            Pending = [id],
        };
    }

    private static Task UseContextAsync(FutureState state, Func<BehaviorContext<FutureState, Signal>, Task> callback)
    {
        var machine = new ContextMachine();
        return FutureBehaviorContextFactory.UseAsync(
            machine,
            machine.SignalReceived,
            state,
            new Signal("input"),
            callback,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }

        public Event<Signal> SignalReceived { get; private set; } = null!;
    }

    public sealed record Signal(string Value);

    public sealed record Command(string Value);

    public sealed class TerminalMessage
    {
        public TerminalMessage()
        {
        }

        public TerminalMessage(string value)
        {
            Value = value;
        }

        public string Value { get; init; } = "";
    }
}
