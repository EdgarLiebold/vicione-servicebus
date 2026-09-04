using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineStateStorageTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative, StateStorageKind.Raw)]
    [InlineData(StateMachineConstructionStyle.Declarative, StateStorageKind.String)]
    [InlineData(StateMachineConstructionStyle.Declarative, StateStorageKind.Integer)]
    [InlineData(StateMachineConstructionStyle.Dynamic, StateStorageKind.Raw)]
    [InlineData(StateMachineConstructionStyle.Dynamic, StateStorageKind.String)]
    [InlineData(StateMachineConstructionStyle.Dynamic, StateStorageKind.Integer)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "raw-string-int-storage-and-expression-matrix")]
    public async Task StateStorage_RoundTripsTheExactRepresentationAndPredicateTruthTableAsync(
        StateMachineConstructionStyle style,
        StateStorageKind storageKind)
    {
        switch (storageKind)
        {
            case StateStorageKind.Raw:
                await VerifyRawStorageAsync(style);
                break;
            case StateStorageKind.String:
                await VerifyStringStorageAsync(style);
                break;
            case StateStorageKind.Integer:
                await VerifyIntegerStorageAsync(style);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, "Unknown storage kind.");
        }
    }

    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative)]
    [InlineData(StateMachineConstructionStyle.Dynamic)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-STORAGE", "json-state-name-round-trip")]
    public async Task JsonRoundTrip_ResolvesTheStoredStateBackToTheOwningMachineAsync(
        StateMachineConstructionStyle style)
    {
        JsonScenario scenario = CreateJsonScenario(style);
        var instance = new JsonStateInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Decide, new Decision(true));

        var options = new JsonSerializerOptions
        {
            Converters = { new MachineStateConverter(scenario.Machine) },
        };
        string json = JsonSerializer.Serialize(instance, options);
        JsonStateInstance? restored = JsonSerializer.Deserialize<JsonStateInstance>(json, options);

        Assert.Contains("\"CurrentState\":\"True\"", json, StringComparison.Ordinal);
        Assert.NotNull(restored);
        Assert.Equal(instance.CorrelationId, restored.CorrelationId);
        Assert.Same(scenario.True, restored.CurrentState);
        Assert.Same(scenario.True, await StateMachineTestExecution.GetStateAsync(scenario.Machine, restored));
    }

    private static async Task VerifyRawStorageAsync(StateMachineConstructionStyle style)
    {
        RawScenario scenario = CreateRawScenario(style);
        var instance = new RawStateInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Start);

        Assert.Same(scenario.Running, instance.CurrentState);
        Assert.Same(scenario.Running, await StateMachineTestExecution.GetStateAsync(scenario.Machine, instance));
        AssertPredicateTruthTable(scenario.Machine, instance, scenario.Running);
    }

    private static async Task VerifyStringStorageAsync(StateMachineConstructionStyle style)
    {
        StringScenario scenario = CreateStringScenario(style);
        var instance = new StringStateInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Start);

        Assert.Equal("Running", instance.CurrentState);
        Assert.Same(scenario.Running, await StateMachineTestExecution.GetStateAsync(scenario.Machine, instance));
        AssertPredicateTruthTable(scenario.Machine, instance, scenario.Running);
    }

    private static async Task VerifyIntegerStorageAsync(StateMachineConstructionStyle style)
    {
        IntegerScenario scenario = CreateIntegerScenario(style);
        var instance = new IntegerStateInstance();

        await StateMachineTestExecution.RaiseAsync(scenario.Machine, instance, scenario.Start);

        Assert.Equal(3, instance.CurrentState);
        Assert.Same(scenario.Running, await StateMachineTestExecution.GetStateAsync(scenario.Machine, instance));
        AssertPredicateTruthTable(scenario.Machine, instance, scenario.Running);
    }

    private static void AssertPredicateTruthTable<TInstance>(
        ViciOneServiceBusStateMachine<TInstance> machine,
        TInstance instance,
        State running)
        where TInstance : class, SagaStateMachineInstance
    {
        Expression<Func<TInstance, bool>> currentExpression = machine.Accessor.GetStateExpression(running);
        Expression<Func<TInstance, bool>> initialExpression = machine.Accessor.GetStateExpression(machine.Initial);
        var negatedInitial = Expression.Lambda<Func<TInstance, bool>>(
            Expression.Not(initialExpression.Body),
            initialExpression.Parameters);

        Assert.True(currentExpression.Compile()(instance));
        Assert.False(initialExpression.Compile()(instance));
        Assert.True(negatedInitial.Compile()(instance));
    }

    private static RawScenario CreateRawScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeRawMachine();
            return new RawScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Start);
        }

        State running = null!;
        Event start = null!;
        ViciOneServiceBusStateMachine<RawStateInstance> machine = ViciOneServiceBusStateMachine<RawStateInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Start", out start)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(running)));
        return new RawScenario(machine, running, start);
    }

    private static StringScenario CreateStringScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeStringMachine();
            return new StringScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Start);
        }

        State running = null!;
        Event start = null!;
        ViciOneServiceBusStateMachine<StringStateInstance> machine = ViciOneServiceBusStateMachine<StringStateInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Start", out start)
            .InstanceState(instance => instance.CurrentState)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(running)));
        return new StringScenario(machine, running, start);
    }

    private static IntegerScenario CreateIntegerScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeIntegerMachine();
            return new IntegerScenario(declarativeMachine, declarativeMachine.Running, declarativeMachine.Start);
        }

        State running = null!;
        Event start = null!;
        ViciOneServiceBusStateMachine<IntegerStateInstance> machine = ViciOneServiceBusStateMachine<IntegerStateInstance>.New(builder => builder
            .State("Running", out running)
            .Event("Start", out start)
            .InstanceState(instance => instance.CurrentState, running)
            .Initially()
            .When(start, behavior => behavior.TransitionTo(running)));
        return new IntegerScenario(machine, running, start);
    }

    private static JsonScenario CreateJsonScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeJsonMachine();
            return new JsonScenario(
                declarativeMachine,
                declarativeMachine.True,
                declarativeMachine.False,
                declarativeMachine.Decide);
        }

        State @true = null!;
        State @false = null!;
        Event<Decision> decide = null!;
        ViciOneServiceBusStateMachine<JsonStateInstance> machine = ViciOneServiceBusStateMachine<JsonStateInstance>.New(builder => builder
            .State("True", out @true)
            .State("False", out @false)
            .Event("Decide", out decide)
            .InstanceState(instance => instance.CurrentState!)
            .Initially()
            .When(decide, context => context.Message.Value, behavior => behavior.TransitionTo(@true))
            .When(decide, context => !context.Message.Value, behavior => behavior.TransitionTo(@false)));
        return new JsonScenario(machine, @true, @false, decide);
    }

    private sealed record RawScenario(
        ViciOneServiceBusStateMachine<RawStateInstance> Machine,
        State Running,
        Event Start);

    private sealed record StringScenario(
        ViciOneServiceBusStateMachine<StringStateInstance> Machine,
        State Running,
        Event Start);

    private sealed record IntegerScenario(
        ViciOneServiceBusStateMachine<IntegerStateInstance> Machine,
        State Running,
        Event Start);

    private sealed record JsonScenario(
        ViciOneServiceBusStateMachine<JsonStateInstance> Machine,
        State True,
        State False,
        Event<Decision> Decide);

    public enum StateStorageKind
    {
        Raw,
        String,
        Integer,
    }

    public sealed record Decision(bool Value);

    private sealed class RawStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }
    }

    private sealed class StringStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public string CurrentState { get; private set; } = string.Empty;
    }

    private sealed class IntegerStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public int CurrentState { get; private set; }
    }

    private sealed class JsonStateInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public State? CurrentState { get; set; }
    }

    private sealed class DeclarativeRawMachine : ViciOneServiceBusStateMachine<RawStateInstance>
    {
        public DeclarativeRawMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class DeclarativeStringMachine : ViciOneServiceBusStateMachine<StringStateInstance>
    {
        public DeclarativeStringMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class DeclarativeIntegerMachine : ViciOneServiceBusStateMachine<IntegerStateInstance>
    {
        public DeclarativeIntegerMachine()
        {
            InstanceState(instance => instance.CurrentState, Running);
            Initially(When(Start).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class DeclarativeJsonMachine : ViciOneServiceBusStateMachine<JsonStateInstance>
    {
        public DeclarativeJsonMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Decide, context => context.Message.Value).TransitionTo(True),
                When(Decide, context => !context.Message.Value).TransitionTo(False));
        }

        public State True { get; private set; } = null!;

        public State False { get; private set; } = null!;

        public Event<Decision> Decide { get; private set; } = null!;
    }

    private sealed class MachineStateConverter : JsonConverter<State>
    {
        private readonly StateMachine _machine;

        public MachineStateConverter(StateMachine machine)
        {
            _machine = machine;
        }

        public override State? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? stateName = reader.GetString();
            return string.IsNullOrWhiteSpace(stateName) ? null : _machine.GetState(stateName);
        }

        public override void Write(Utf8JsonWriter writer, State value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Name);
    }
}
