using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineTelephoneTests
{
    [Theory]
    [InlineData(StateMachineConstructionStyle.Declarative, TelephonePath.ConnectedHangUp)]
    [InlineData(StateMachineConstructionStyle.Declarative, TelephonePath.HoldResumeHangUp)]
    [InlineData(StateMachineConstructionStyle.Declarative, TelephonePath.HoldHangUp)]
    [InlineData(StateMachineConstructionStyle.Dynamic, TelephonePath.ConnectedHangUp)]
    [InlineData(StateMachineConstructionStyle.Dynamic, TelephonePath.HoldResumeHangUp)]
    [InlineData(StateMachineConstructionStyle.Dynamic, TelephonePath.HoldHangUp)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-TELEPHONE", "connected-superstate-lifecycle-matrix")]
    public async Task ConnectedSuperstate_EntersAndLeavesExactlyOnceAcrossEveryCallPathAsync(
        StateMachineConstructionStyle style,
        TelephonePath path)
    {
        TelephoneScenario scenario = CreateScenario(style);
        var phone = new TelephoneInstance();
        var states = new List<string>();

        await RaiseAndRecordAsync(scenario, phone, scenario.ServiceEstablished, new ServiceEstablished("555-1212"), states);
        await RaiseAndRecordAsync(scenario, phone, scenario.CallDialed, states);
        await RaiseAndRecordAsync(scenario, phone, scenario.CallConnected, states);

        switch (path)
        {
            case TelephonePath.ConnectedHangUp:
                await RaiseAndRecordAsync(scenario, phone, scenario.HungUp, states);
                Assert.Equal(["OffHook", "Ringing", "Connected", "OffHook"], states);
                break;
            case TelephonePath.HoldResumeHangUp:
                await RaiseAndRecordAsync(scenario, phone, scenario.PlacedOnHold, states);
                await RaiseAndRecordAsync(scenario, phone, scenario.TakenOffHold, states);
                await RaiseAndRecordAsync(scenario, phone, scenario.HungUp, states);
                Assert.Equal(["OffHook", "Ringing", "Connected", "OnHold", "Connected", "OffHook"], states);
                break;
            case TelephonePath.HoldHangUp:
                await RaiseAndRecordAsync(scenario, phone, scenario.PlacedOnHold, states);
                await RaiseAndRecordAsync(scenario, phone, scenario.HungUp, states);
                Assert.Equal(["OffHook", "Ringing", "Connected", "OnHold", "OffHook"], states);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(path), path, "Unknown telephone path.");
        }

        Assert.Equal("555-1212", phone.Number);
        Assert.Equal(1, phone.ConnectedEnterCount);
        Assert.Equal(1, phone.ConnectedLeaveCount);
        Assert.Equal(["connected-enter", "connected-leave"], phone.LifecycleMarkers);
        Assert.Equal("OffHook", phone.CurrentState);
        Assert.Same(scenario.OffHook, await StateMachineTestExecution.GetStateAsync(scenario.Machine, phone));
    }

    private static async Task RaiseAndRecordAsync(
        TelephoneScenario scenario,
        TelephoneInstance phone,
        Event @event,
        List<string> states)
    {
        await StateMachineTestExecution.RaiseAsync(scenario.Machine, phone, @event);
        states.Add((await StateMachineTestExecution.GetStateAsync(scenario.Machine, phone)).Name);
    }

    private static async Task RaiseAndRecordAsync(
        TelephoneScenario scenario,
        TelephoneInstance phone,
        Event<ServiceEstablished> @event,
        ServiceEstablished message,
        List<string> states)
    {
        await StateMachineTestExecution.RaiseAsync(scenario.Machine, phone, @event, message);
        states.Add((await StateMachineTestExecution.GetStateAsync(scenario.Machine, phone)).Name);
    }

    private static TelephoneScenario CreateScenario(StateMachineConstructionStyle style)
    {
        if (style == StateMachineConstructionStyle.Declarative)
        {
            var declarativeMachine = new DeclarativeTelephoneMachine();
            return new TelephoneScenario(
                declarativeMachine,
                declarativeMachine.OffHook,
                declarativeMachine.ServiceEstablished,
                declarativeMachine.CallDialed,
                declarativeMachine.CallConnected,
                declarativeMachine.HungUp,
                declarativeMachine.PlacedOnHold,
                declarativeMachine.TakenOffHold);
        }

        State offHook = null!;
        State ringing = null!;
        State<TelephoneInstance> connected = null!;
        State<TelephoneInstance> onHold = null!;
        State phoneDestroyed = null!;
        Event<ServiceEstablished> serviceEstablished = null!;
        Event callDialed = null!;
        Event hungUp = null!;
        Event callConnected = null!;
        Event leftMessage = null!;
        Event placedOnHold = null!;
        Event takenOffHold = null!;
        Event phoneHurledAgainstWall = null!;
        ViciOneServiceBusStateMachine<TelephoneInstance> machine = ViciOneServiceBusStateMachine<TelephoneInstance>.New(builder => builder
            .State("OffHook", out offHook)
            .State("Ringing", out ringing)
            .State("Connected", out connected)
            .State("PhoneDestroyed", out phoneDestroyed)
            .Event("ServiceEstablished", out serviceEstablished)
            .Event("CallDialed", out callDialed)
            .Event("HungUp", out hungUp)
            .Event("CallConnected", out callConnected)
            .Event("LeftMessage", out leftMessage)
            .Event("PlacedOnHold", out placedOnHold)
            .Event("TakenOffHold", out takenOffHold)
            .Event("PhoneHurledAgainstWall", out phoneHurledAgainstWall)
            .InstanceState(instance => instance.CurrentState!)
            .SubState("OnHold", connected, out onHold)
            .Initially()
            .When(serviceEstablished, behavior => behavior
                .Then(context => context.Saga.Number = context.Message.Digits)
                .TransitionTo(offHook))
            .During(offHook)
            .When(callDialed, behavior => behavior.TransitionTo(ringing))
            .During(ringing)
            .When(hungUp, behavior => behavior.TransitionTo(offHook))
            .When(callConnected, behavior => behavior.TransitionTo(connected))
            .During(connected)
            .When(leftMessage, behavior => behavior.TransitionTo(offHook))
            .When(hungUp, behavior => behavior.TransitionTo(offHook))
            .When(placedOnHold, behavior => behavior.TransitionTo(onHold))
            .During(onHold)
            .When(takenOffHold, behavior => behavior.TransitionTo(connected))
            .When(phoneHurledAgainstWall, behavior => behavior.TransitionTo(phoneDestroyed))
            .DuringAny()
            .When(connected.Enter, behavior => behavior.Then(context => MarkConnectedEnter(context.Saga)))
            .When(connected.Leave, behavior => behavior.Then(context => MarkConnectedLeave(context.Saga))));

        return new TelephoneScenario(machine, offHook, serviceEstablished, callDialed, callConnected,
            hungUp, placedOnHold, takenOffHold);
    }

    private static void MarkConnectedEnter(TelephoneInstance instance)
    {
        instance.ConnectedEnterCount++;
        instance.LifecycleMarkers.Add("connected-enter");
    }

    private static void MarkConnectedLeave(TelephoneInstance instance)
    {
        instance.ConnectedLeaveCount++;
        instance.LifecycleMarkers.Add("connected-leave");
    }

    private sealed class DeclarativeTelephoneMachine : ViciOneServiceBusStateMachine<TelephoneInstance>
    {
        public DeclarativeTelephoneMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            SubState(() => OnHold, Connected);

            Initially(When(ServiceEstablished)
                .Then(context => context.Saga.Number = context.Message.Digits)
                .TransitionTo(OffHook));
            During(OffHook, When(CallDialed).TransitionTo(Ringing));
            During(Ringing,
                When(HungUp).TransitionTo(OffHook),
                When(CallConnected).TransitionTo(Connected));
            During(Connected,
                When(LeftMessage).TransitionTo(OffHook),
                When(HungUp).TransitionTo(OffHook),
                When(PlacedOnHold).TransitionTo(OnHold));
            During(OnHold,
                When(TakenOffHold).TransitionTo(Connected),
                When(PhoneHurledAgainstWall).TransitionTo(PhoneDestroyed));
            DuringAny(
                When(Connected.Enter).Then(context => MarkConnectedEnter(context.Saga)),
                When(Connected.Leave).Then(context => MarkConnectedLeave(context.Saga)));
        }

        public State OffHook { get; private set; } = null!;
        public State Ringing { get; private set; } = null!;
        public State Connected { get; private set; } = null!;
        public State OnHold { get; private set; } = null!;
        public State PhoneDestroyed { get; private set; } = null!;
        public Event<ServiceEstablished> ServiceEstablished { get; private set; } = null!;
        public Event CallDialed { get; private set; } = null!;
        public Event HungUp { get; private set; } = null!;
        public Event CallConnected { get; private set; } = null!;
        public Event LeftMessage { get; private set; } = null!;
        public Event PlacedOnHold { get; private set; } = null!;
        public Event TakenOffHold { get; private set; } = null!;
        public Event PhoneHurledAgainstWall { get; private set; } = null!;
    }

    public enum TelephonePath
    {
        ConnectedHangUp,
        HoldResumeHangUp,
        HoldHangUp,
    }

    private sealed class TelephoneInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public string? CurrentState { get; set; }
        public string? Number { get; set; }
        public int ConnectedEnterCount { get; set; }
        public int ConnectedLeaveCount { get; set; }
        public List<string> LifecycleMarkers { get; } = [];
    }

    public sealed record ServiceEstablished(string Digits);

    private sealed record TelephoneScenario(
        ViciOneServiceBusStateMachine<TelephoneInstance> Machine,
        State OffHook,
        Event<ServiceEstablished> ServiceEstablished,
        Event CallDialed,
        Event CallConnected,
        Event HungUp,
        Event PlacedOnHold,
        Event TakenOffHold);
}
