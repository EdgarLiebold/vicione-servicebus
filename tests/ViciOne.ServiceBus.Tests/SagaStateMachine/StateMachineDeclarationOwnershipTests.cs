using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineDeclarationOwnershipTests
{
    [Theory]
    [InlineData(DeclarationForm.DirectState)]
    [InlineData(DeclarationForm.NestedState)]
    [InlineData(DeclarationForm.DirectSubState)]
    [InlineData(DeclarationForm.NestedSubState)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "redeclaration-keeps-registered-state-owner")]
    public void Redeclaration_RestoresItsOwnStateAfterAPropertyReceivesAForeignSameNameState(
        DeclarationForm form)
    {
        var owner = new OwnershipMachine();
        var foreignMachine = new OwnershipMachine();
        IState original = owner.Property(form);
        IState foreign = foreignMachine.Property(form);
        IState? originalParent = (original as IState<OwnershipInstance>)?.SuperState;
        IEvent[] transitionEvents =
        [original.BeforeEnter, original.Enter, original.Leave, original.AfterLeave];

        Assert.NotSame(original, foreign);
        Assert.Equal(original.Name, foreign.Name);
        owner.ReplacePropertyAndRedeclare(form, foreign);

        IState restored = owner.Property(form);
        Assert.Same(original, restored);
        Assert.Same(original, owner.GetState(original.Name));
        Assert.NotSame(foreign, restored);
        Assert.Same(originalParent, ((IState<OwnershipInstance>)restored).SuperState);
        foreach (IEvent transitionEvent in transitionEvents)
            Assert.Same(transitionEvent, ((IStateMachine)owner).GetEvent(transitionEvent.Name));
    }

    [Theory]
    [InlineData(DeclarationForm.DirectSubState)]
    [InlineData(DeclarationForm.NestedSubState)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "substate-reparent-replaces-state-and-events")]
    public void ReparentingAPropertySubstate_ReplacesItsStateAndTransitionEvents(DeclarationForm form)
    {
        var machine = new OwnershipMachine();
        IState original = machine.Property(form);
        IEvent originalEnter = original.Enter;
        var originalParent = Assert.IsType<OwnershipMachine.StateMachineState>(
            ((IState<OwnershipInstance>)original).SuperState);

        machine.ReparentPropertySubstate(form);

        IState replacement = machine.Property(form);
        Assert.NotSame(original, replacement);
        Assert.Same(machine.Alternate, ((IState<OwnershipInstance>)replacement).SuperState);
        Assert.Same(replacement, machine.GetState(replacement.Name));
        Assert.NotSame(originalEnter, replacement.Enter);
        Assert.Same(replacement.Enter, ((IStateMachine)machine).GetEvent(replacement.Enter.Name));
        Assert.False(originalParent.HasState((IState<OwnershipInstance>)replacement));
        Assert.True(Assert.IsType<OwnershipMachine.StateMachineState>(machine.Alternate)
            .HasState((IState<OwnershipInstance>)replacement));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "named-substate-follows-replaced-parent-instance")]
    public void NamedSubstate_RebindsWhenItsRegisteredParentIsReplacedWithTheSameName()
    {
        var machine = new OwnershipMachine();
        IState<OwnershipInstance> originalParent = machine.GetState(nameof(OwnershipMachine.Running));
        IState<OwnershipInstance> originalChild = machine.DeclareNamedChild();
        IState<OwnershipInstance> directChild = machine.GetState(nameof(OwnershipMachine.Child));
        IState<OwnershipInstance> nestedChild = machine.GetState("Group.Child");
        IEvent originalEnter = originalChild.Enter;

        machine.ReparentRunning();
        IState<OwnershipInstance> currentParent = machine.GetState(nameof(OwnershipMachine.Running));
        IState<OwnershipInstance> currentChild = machine.DeclareNamedChild();

        Assert.NotSame(originalParent, currentParent);
        Assert.Same(machine.Alternate, currentParent.SuperState);
        Assert.Same(originalChild, currentChild);
        Assert.Same(currentParent, currentChild.SuperState);
        Assert.Same(currentParent, directChild.SuperState);
        Assert.Same(currentParent, nestedChild.SuperState);
        Assert.Same(currentChild, machine.GetState(currentChild.Name));
        Assert.Same(originalEnter, currentChild.Enter);
        Assert.Same(currentChild.Enter, ((IStateMachine)machine).GetEvent(currentChild.Enter.Name));
        Assert.False(Assert.IsType<OwnershipMachine.StateMachineState>(originalParent).HasState(currentChild));
        Assert.False(Assert.IsType<OwnershipMachine.StateMachineState>(originalParent).HasState(directChild));
        Assert.False(Assert.IsType<OwnershipMachine.StateMachineState>(originalParent).HasState(nestedChild));
        Assert.True(Assert.IsType<OwnershipMachine.StateMachineState>(currentParent).HasState(currentChild));
    }

    [Theory]
    [InlineData(DeclarationForm.DirectSubState)]
    [InlineData(DeclarationForm.NestedSubState)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "descendant-parent-cycle-rejected-atomically")]
    public void PropertyState_CannotBeReparentedUnderItsOwnDescendant(DeclarationForm form)
    {
        var machine = new OwnershipMachine();
        IState original = form == DeclarationForm.DirectSubState ? machine.Running : machine.Group.Child;
        IState descendant = form == DeclarationForm.DirectSubState ? machine.Child : machine.Group.Grandchild;
        IState? originalParent = ((IState<OwnershipInstance>)original).SuperState;
        IEvent originalEnter = original.Enter;

        ArgumentException error = Assert.Throws<ArgumentException>(() => machine.AttemptDescendantReparent(form));
        Assert.Equal("superState", error.ParamName);

        Assert.Same(original, form == DeclarationForm.DirectSubState ? machine.Running : machine.Group.Child);
        Assert.Same(original, machine.GetState(original.Name));
        Assert.Same(originalParent, ((IState<OwnershipInstance>)original).SuperState);
        Assert.Same(original, ((IState<OwnershipInstance>)descendant).SuperState);
        Assert.Same(originalEnter, ((IStateMachine)machine).GetEvent(originalEnter.Name));
        Assert.True(Assert.IsType<OwnershipMachine.StateMachineState>(original)
            .HasState((IState<OwnershipInstance>)descendant));
        Assert.False(Assert.IsType<OwnershipMachine.StateMachineState>(descendant)
            .HasState((IState<OwnershipInstance>)original));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DEFINITION", "named-descendant-parent-cycle-rejected-atomically")]
    public void NamedState_CannotBeReparentedUnderItsOwnNamedDescendant()
    {
        var machine = new OwnershipMachine();
        IState<OwnershipInstance> parent = machine.DeclareNamedChild();
        IState<OwnershipInstance> descendant = machine.DeclareNamedGrandchild(parent);
        IEvent originalEnter = parent.Enter;

        ArgumentException error = Assert.Throws<ArgumentException>(() => machine.AttemptNamedCycle(descendant));
        Assert.Equal("superState", error.ParamName);

        Assert.Same(parent, machine.GetState(parent.Name));
        Assert.Same(machine.Running, parent.SuperState);
        Assert.Same(parent, descendant.SuperState);
        Assert.Same(originalEnter, ((IStateMachine)machine).GetEvent(originalEnter.Name));
        Assert.True(Assert.IsType<OwnershipMachine.StateMachineState>(parent).HasState(descendant));
        Assert.False(Assert.IsType<OwnershipMachine.StateMachineState>(descendant).HasState(parent));
    }

    public enum DeclarationForm
    {
        DirectState,
        NestedState,
        DirectSubState,
        NestedSubState,
    }

    public sealed class OwnershipInstance : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class StateGroup
    {
        public IState Ready { get; set; } = null!;
        public IState Child { get; set; } = null!;
        public IState Grandchild { get; set; } = null!;
    }

    public sealed class OwnershipMachine : ViciOneServiceBusStateMachine<OwnershipInstance>
    {
        public OwnershipMachine()
        {
            SubState(() => Child, Running);
            State(() => Group, group => group.Ready);
            SubState(() => Group, group => group.Child, Running);
            SubState(() => Group, group => group.Grandchild, Group.Child);
        }

        public IState Running { get; private set; } = null!;
        public IState Alternate { get; private set; } = null!;
        public IState Child { get; private set; } = null!;
        public StateGroup Group { get; } = new();

        public IState<OwnershipInstance> DeclareNamedChild() => SubState("NamedChild", Running);

        public IState<OwnershipInstance> DeclareNamedGrandchild(IState parent) =>
            SubState("NamedGrandchild", parent);

        public void AttemptNamedCycle(IState descendant) => SubState("NamedChild", descendant);

        public void AttemptDescendantReparent(DeclarationForm form)
        {
            switch (form)
            {
                case DeclarationForm.DirectSubState:
                    SubState(() => Running, Child);
                    break;
                case DeclarationForm.NestedSubState:
                    SubState(() => Group, group => group.Child, Group.Grandchild);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(form));
            }
        }

        public void ReparentRunning() => SubState(() => Running, Alternate);

        public void ReparentPropertySubstate(DeclarationForm form)
        {
            switch (form)
            {
                case DeclarationForm.DirectSubState:
                    SubState(() => Child, Alternate);
                    break;
                case DeclarationForm.NestedSubState:
                    SubState(() => Group, group => group.Child, Alternate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(form));
            }
        }

        public IState Property(DeclarationForm form) => form switch
        {
            DeclarationForm.DirectState => Running,
            DeclarationForm.NestedState => Group.Ready,
            DeclarationForm.DirectSubState => Child,
            DeclarationForm.NestedSubState => Group.Child,
            _ => throw new ArgumentOutOfRangeException(nameof(form)),
        };

        public void ReplacePropertyAndRedeclare(DeclarationForm form, IState replacement)
        {
            switch (form)
            {
                case DeclarationForm.DirectState:
                    Running = replacement;
                    State(() => Running);
                    break;
                case DeclarationForm.NestedState:
                    Group.Ready = replacement;
                    State(() => Group, group => group.Ready);
                    break;
                case DeclarationForm.DirectSubState:
                    Child = replacement;
                    SubState(() => Child, Running);
                    break;
                case DeclarationForm.NestedSubState:
                    Group.Child = replacement;
                    SubState(() => Group, group => group.Child, Running);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(form));
            }
        }
    }
}
