using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCompositeDeclarationAtomicityTests
{
    [Theory]
    [InlineData(InvalidConstituents.Null)]
    [InlineData(InvalidConstituents.Empty)]
    [InlineData(InvalidConstituents.TooMany)]
    [InlineData(InvalidConstituents.Uninitialized)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "invalid-property-declaration-preserves-event")]
    public async Task InvalidPropertyDeclaration_PreservesTheOriginalEventAndAllowsAWorkingCompositeAsync(
        InvalidConstituents invalid)
    {
        var machine = new CompositeDeclarationMachine();
        IEvent original = machine.Combined;
        IEvent[] before = machine.Events.ToArray();

        Exception? error = Record.Exception(() => machine.DeclareProperty(InvalidEvents(machine.First, invalid)));

        AssertInvalid(error, invalid);
        Assert.Same(original, machine.Combined);
        Assert.Same(original, ((IStateMachine)machine).GetEvent(nameof(machine.Combined)));
        Assert.Equal(before, machine.Events);
        Assert.False(machine.IsCompositeEvent(original));

        machine.DeclareProperty([machine.First]);
        await AssertWorkingCompositeAsync(machine, machine.Combined);
    }

    [Theory]
    [InlineData(InvalidConstituents.Null)]
    [InlineData(InvalidConstituents.Empty)]
    [InlineData(InvalidConstituents.TooMany)]
    [InlineData(InvalidConstituents.Uninitialized)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "invalid-named-declaration-leaves-no-ghost")]
    public async Task InvalidNamedDeclaration_LeavesNoGhostAndAllowsAWorkingCompositeAsync(
        InvalidConstituents invalid)
    {
        var machine = new CompositeDeclarationMachine();
        IEvent[] before = machine.Events.ToArray();

        Exception? error = Record.Exception(() => machine.DeclareNamed(InvalidEvents(machine.First, invalid)));

        AssertInvalid(error, invalid);
        Assert.Equal(before, machine.Events);
        Assert.Throws<UnknownEventException>(() => ((IStateMachine)machine).GetEvent("NamedCombined"));

        IEvent valid = machine.DeclareNamed([machine.First]);
        await AssertWorkingCompositeAsync(machine, valid);
    }

    [Theory]
    [InlineData(InvalidConstituents.Null)]
    [InlineData(InvalidConstituents.Empty)]
    [InlineData(InvalidConstituents.TooMany)]
    [InlineData(InvalidConstituents.Uninitialized)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "invalid-existing-event-attachment-preserves-definition")]
    public async Task InvalidExistingEventAttachment_LeavesTheEventUsableForAValidCompositeAsync(
        InvalidConstituents invalid)
    {
        var machine = new CompositeDeclarationMachine();
        IEvent existing = machine.Combined;
        IEvent[] before = machine.Events.ToArray();

        Exception? error = Record.Exception(() => machine.AttachExisting(existing, InvalidEvents(machine.First, invalid)));

        AssertInvalid(error, invalid);
        Assert.Same(existing, machine.Combined);
        Assert.Same(existing, ((IStateMachine)machine).GetEvent(nameof(machine.Combined)));
        Assert.Equal(before, machine.Events);
        Assert.False(machine.IsCompositeEvent(existing));

        machine.AttachExisting(existing, [machine.First]);
        await AssertWorkingCompositeAsync(machine, existing);
    }

    private static IEvent[]? InvalidEvents(IEvent first, InvalidConstituents invalid) => invalid switch
    {
        InvalidConstituents.Null => null,
        InvalidConstituents.Empty => [],
        InvalidConstituents.TooMany => Enumerable.Repeat(first, 32).ToArray(),
        InvalidConstituents.Uninitialized => [first, null!],
        _ => throw new ArgumentOutOfRangeException(nameof(invalid)),
    };

    private static void AssertInvalid(Exception? error, InvalidConstituents invalid)
    {
        if (invalid == InvalidConstituents.Null)
        {
            Assert.Equal("events", Assert.IsType<ArgumentNullException>(error).ParamName);
            return;
        }

        ArgumentException argument = Assert.IsType<ArgumentException>(error);
        Assert.Contains(invalid switch
        {
            InvalidConstituents.Empty => "At least one event",
            InvalidConstituents.TooMany => "No more than 31 events",
            InvalidConstituents.Uninitialized => "not yet been initialized",
            _ => throw new ArgumentOutOfRangeException(nameof(invalid)),
        }, argument.Message);
    }

    private static async Task AssertWorkingCompositeAsync(CompositeDeclarationMachine machine, IEvent combined)
    {
        var instance = new CompositeDeclarationInstance();
        machine.HandleCombined(combined);

        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.Start);
        await StateMachineTestExecution.RaiseAsync(machine, instance, machine.First);

        Assert.Equal(1, instance.CompositeCount);
        Assert.Equal(1, instance.Status.Bits);
        Assert.Same(machine.Waiting, await StateMachineTestExecution.GetStateAsync(machine, instance));
        Assert.True(machine.IsCompositeEvent(combined));
        Assert.Same(combined, ((IStateMachine)machine).GetEvent(combined.Name));
    }

    public enum InvalidConstituents
    {
        Null,
        Empty,
        TooMany,
        Uninitialized,
    }

    private sealed class CompositeDeclarationMachine : ViciOneServiceBusStateMachine<CompositeDeclarationInstance>
    {
        public CompositeDeclarationMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Waiting));
        }

        public IState Waiting { get; private set; } = null!;
        public IEvent Start { get; private set; } = null!;
        public IEvent First { get; private set; } = null!;
        public IEvent Combined { get; private set; } = null!;

        public void DeclareProperty(IEvent[]? events) =>
            CompositeEvent(() => Combined, instance => instance.Status, events!);

        public IEvent DeclareNamed(IEvent[]? events) =>
            CompositeEvent("NamedCombined", instance => instance.Status, CompositeEventOptions.None, events!);

        public void AttachExisting(IEvent existing, IEvent[]? events) =>
            CompositeEvent(existing, instance => instance.Status, events!);

        public void HandleCombined(IEvent combined) =>
            During(Waiting, When(combined).Then(context => context.Saga.CompositeCount++));
    }

    private sealed class CompositeDeclarationInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public IState CurrentState { get; set; } = null!;
        public CompositeEventStatus Status { get; set; }
        public int CompositeCount { get; set; }
    }
}
