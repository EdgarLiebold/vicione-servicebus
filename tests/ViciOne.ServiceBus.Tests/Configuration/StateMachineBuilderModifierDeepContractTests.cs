using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class StateMachineBuilderModifierDeepContractTests
{
    static MachineMembers SharedMembers { get; } = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "builder-required-owner-boundaries-and-machine-state-identity")]
    public void Constructors_RejectMissingOwnersAndPreserveMachineStateIdentity()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        Action<IEventActivities<ProbeState>[]> committer = _ => { };

        AssertConstructorArgument("machine", () => CreateModifier(null!));
        AssertConstructorArgument("machine", () => CreateBuilder(null!, modifier, committer));
        AssertConstructorArgument("modifier", () => CreateBuilder(machine, null!, committer));
        AssertConstructorArgument("committer", () => CreateBuilder(machine, modifier, null!));

        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, committer);

        Assert.Same(machine.Initial, modifier.Initial);
        Assert.Same(machine.Final, modifier.Final);
        Assert.Same(modifier.Initial, builder.Initial);
        Assert.Same(modifier.Final, builder.Final);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "ordered-snapshot-single-commit-and-closed-builder")]
    public void CommitActivities_CommitsOrderedSnapshotExactlyOnceAndClosesBuilder()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        modifier.Event("First", out IEvent first);
        modifier.Event("Second", out IEvent second);
        var commits = new List<IEventActivities<ProbeState>[]>();
        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, commits.Add);

        Assert.Same(builder, builder.Ignore(first));
        Assert.Same(builder, builder.Ignore(second));

        Assert.Same(modifier, builder.CommitActivities());
        Assert.True(builder.IsCommitted);
        IEventActivities<ProbeState>[] snapshot = Assert.Single(commits);
        Assert.Equal([first, second], snapshot.Select(GetEvent));

        Assert.Same(modifier, builder.CommitActivities());
        Assert.Single(commits);
        InvalidOperationException closed = Assert.Throws<InvalidOperationException>(() => builder.Ignore(first));
        Assert.Equal("The state machine event activities have already been committed.", closed.Message);
        Assert.Equal([first, second], snapshot.Select(GetEvent));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "failed-commit-retry-and-successful-publication")]
    public void CommitActivities_FailedAttemptStaysRetryableAndPublishesOnlyAfterSuccess()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        modifier.Event("Retryable", out IEvent retryable);
        var failure = new InvalidOperationException("commit failed");
        var snapshots = new List<IEventActivities<ProbeState>[]>();
        var attempts = 0;
        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, activities =>
        {
            snapshots.Add(activities);
            if (attempts++ == 0)
                throw failure;
        });
        builder.Ignore(retryable);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => builder.CommitActivities()));
        Assert.False(builder.IsCommitted);

        Assert.Same(modifier, builder.CommitActivities());
        Assert.True(builder.IsCommitted);
        Assert.Equal(2, snapshots.Count);
        Assert.NotSame(snapshots[0], snapshots[1]);
        Assert.All(snapshots, snapshot => Assert.Same(retryable, GetEvent(Assert.Single(snapshot))));

        builder.CommitActivities();
        Assert.Equal(2, snapshots.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "modifier-pending-order-failure-isolation-and-retry")]
    public void Apply_CommitsOnlyPendingBuildersInOrderAndRetriesOnlyTheFailure()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        var calls = new List<string>();
        IStateMachineEventActivitiesBuilder<ProbeState> first = CreateRecordingBuilder(modifier, "first", calls);
        IStateMachineEventActivitiesBuilder<ProbeState> alreadyCommitted = CreateRecordingBuilder(modifier, "committed", calls, committed: true);
        var failure = new InvalidOperationException("third failed");
        IStateMachineEventActivitiesBuilder<ProbeState> third = CreateRecordingBuilder(modifier, "third", calls, failure);
        AddTrackedBuilder(modifier, first);
        AddTrackedBuilder(modifier, alreadyCommitted);
        AddTrackedBuilder(modifier, third);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(modifier.Apply));
        Assert.Equal(["first", "third"], calls);
        Assert.True(first.IsCommitted);
        Assert.True(alreadyCommitted.IsCommitted);
        Assert.False(third.IsCommitted);

        modifier.Apply();
        Assert.Equal(["first", "third", "third"], calls);
        Assert.True(third.IsCommitted);

        modifier.Apply();
        Assert.Equal(["first", "third", "third"], calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "commit-before-forwarding-and-owner-return")]
    public void Forwarding_CommitsBeforeCallingTheModifierAndPreservesOwnerReturn()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        modifier.Event("Forwarded", out IEvent forwarded);
        var observedNames = new List<string>();
        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, activities =>
        {
            Assert.Same(forwarded, GetEvent(Assert.Single(activities)));
            observedNames.Add(((IStateMachine)machine).Name);
        });
        builder.Ignore(forwarded);

        IStateMachineModifier<ProbeState> result = builder.Name("renamed-machine");

        Assert.Same(modifier, result);
        Assert.Equal([nameof(ProbeMachine)], observedNames);
        Assert.Equal("renamed-machine", ((IStateMachine)machine).Name);
        Assert.True(builder.IsCommitted);

        AssertForwarding("Apply", static configured => configured.Apply());
        AssertForwarding("AfterLeave", static configured => configured.AfterLeave(null!, null!));
        AssertForwarding("AfterLeaveAny", static configured => configured.AfterLeaveAny(null!));
        AssertForwarding("BeforeEnter", static configured => configured.BeforeEnter(null!, null!));
        AssertForwarding("BeforeEnterAny", static configured => configured.BeforeEnterAny(null!));
        AssertForwarding("CompositeEvent", static configured => configured.CompositeEvent(
            "status", out _, (Expression<Func<ProbeState, CompositeEventStatus>>)null!, []));
        AssertForwarding("CompositeEvent", static configured => configured.CompositeEvent(
            "status-options", out _, (Expression<Func<ProbeState, CompositeEventStatus>>)null!, default, []));
        AssertForwarding("CompositeEvent", static configured => configured.CompositeEvent(
            "integer", out _, (Expression<Func<ProbeState, int>>)null!, []));
        AssertForwarding("CompositeEvent", static configured => configured.CompositeEvent(
            "integer-options", out _, (Expression<Func<ProbeState, int>>)null!, default, []));
        AssertForwarding("During", static configured => configured.During([]));
        AssertForwarding("DuringAny", static configured => configured.DuringAny());
        AssertForwarding("Event", static configured => configured.Event("plain", out IEvent _));
        AssertForwarding("Event", static configured => configured.Event<ProbeMessage>("typed", out _));
        AssertForwarding("Event", static configured => configured.Event<ProbeMessage>("correlated", _ => { }, out _));
        AssertForwarding("Event", static configured => configured.Event<MachineMembers, ProbeMessage>(null!, null!));
        AssertForwarding("Finally", static configured => configured.Finally(null!));
        AssertForwarding("Initially", static configured => configured.Initially());
        AssertForwarding("InstanceState", static configured => configured.InstanceState(
            (Expression<Func<ProbeState, IState?>>)null!));
        AssertForwarding("InstanceState", static configured => configured.InstanceState(
            (Expression<Func<ProbeState, string>>)null!));
        AssertForwarding("InstanceState", static configured => configured.InstanceState(
            (Expression<Func<ProbeState, int>>)null!, []));
        AssertForwarding("OnUnhandledEvent", static configured => configured.OnUnhandledEvent(null!));
        AssertForwarding("State", static configured => configured.State("typed-state", out IState<ProbeState> _));
        AssertForwarding("State", static configured => configured.State("state", out IState _));
        AssertForwarding("State", static configured => configured.State<MachineMembers>(null!, null!));
        AssertForwarding("SubState", static configured => configured.SubState("sub-state", null!, out IState<ProbeState> _));
        AssertForwarding("SubState", static configured => configured.SubState<MachineMembers>(null!, null!, null!));
        AssertForwarding("WhenEnter", static configured => configured.WhenEnter(null!, null!));
        AssertForwarding("WhenEnterAny", static configured => configured.WhenEnterAny(null!));
        AssertForwarding("WhenLeave", static configured => configured.WhenLeave(null!, null!));
        AssertForwarding("WhenLeaveAny", static configured => configured.WhenLeaveAny(null!));

        ExerciseModifierForwarders();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "activity-input-null-boundaries-and-failure-atomicity")]
    public void ActivityMethods_RejectNullsAndNullCallbackResultsWithoutMutation()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        modifier.Event("Trigger", out IEvent trigger);
        modifier.Event<ProbeMessage>("DataTrigger", out IEvent<ProbeMessage> dataTrigger);
        var commits = new List<IEventActivities<ProbeState>[]>();
        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, commits.Add);

        AssertArgument("event", () => builder.When(null!, binder => binder));
        AssertArgument("configure", () => builder.When(trigger, null!));
        AssertArgument("filter", () => builder.When(trigger, null!, binder => binder));
        AssertArgument("event", () => builder.When<ProbeMessage>(null!, binder => binder));
        AssertArgument("configure", () => builder.When(dataTrigger, null!));
        AssertArgument("filter", () => builder.When(dataTrigger, null!, binder => binder));
        AssertArgument("event", () => builder.Ignore(null!));
        AssertArgument("event", () => builder.Ignore<ProbeMessage>(null!));
        AssertArgument("filter", () => builder.Ignore(dataTrigger, null!));
        InvalidOperationException nullResult = Assert.Throws<InvalidOperationException>(() => builder.When(trigger, _ => null!));
        Assert.Equal("The event activity configuration callback returned null.", nullResult.Message);
        Assert.False(builder.IsCommitted);
        Assert.Empty(commits);

        builder.Ignore(trigger).CommitActivities();

        IEventActivities<ProbeState>[] snapshot = Assert.Single(commits);
        Assert.Same(trigger, GetEvent(Assert.Single(snapshot)));
    }

    static IEvent GetEvent(IEventActivities<ProbeState> activities) => Assert.Single(activities.GetStateActivityBinders()).Event;

    static void AssertForwarding(string expectedMethod, Action<IStateMachineEventActivitiesBuilder<ProbeState>> action)
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = DispatchProxy.Create<IStateMachineModifier<ProbeState>, RecordingModifierProxy>();
        var proxy = (RecordingModifierProxy)(object)modifier;
        var calls = new List<string>();
        proxy.Calls = calls;
        IStateMachineEventActivitiesBuilder<ProbeState> builder = CreateBuilder(machine, modifier, _ => calls.Add("Commit"));
        proxy.ReturnBuilder = builder;

        action(builder);

        Assert.Equal(["Commit", expectedMethod], calls);
        Assert.True(builder.IsCommitted);
    }

    static void ExerciseModifierForwarders()
    {
        var machine = new ProbeMachine();
        IStateMachineModifier<ProbeState> modifier = CreateModifier(machine);
        modifier.Event("FirstSource", out IEvent first);
        modifier.Event("SecondSource", out IEvent second);
        Assert.Same(modifier, modifier.CompositeEvent(
            "StatusOptions", out _, state => state.CompositeStatus, (CompositeEventOptions)0, first, second));
        Assert.Same(modifier, modifier.CompositeEvent(
            "IntegerOptions", out _, state => state.CompositeBits, (CompositeEventOptions)0, first, second));
        Assert.Same(modifier, modifier.Event<ProbeMessage>("Correlated", _ => { }, out _));

        Assert.Same(modifier, modifier.Event<MachineMembers, ProbeMessage>(() => SharedMembers, value => value.Event));
        Assert.NotNull(SharedMembers.Event);
        Assert.Same(modifier, modifier.Finally(binder => binder));
        Assert.Same(modifier, modifier.State<MachineMembers>(() => SharedMembers, value => value.State));
        Assert.NotNull(SharedMembers.State);
        Assert.Same(modifier, modifier.SubState<MachineMembers>(
            () => SharedMembers, value => value.SubState, SharedMembers.State));
        Assert.NotNull(SharedMembers.SubState);
        Assert.Same(modifier, modifier.WhenEnter(SharedMembers.State, binder => binder));
        Assert.Same(modifier, modifier.WhenEnterAny(binder => binder));
        Assert.Same(modifier, modifier.WhenLeave(SharedMembers.State, binder => binder));
        Assert.Same(modifier, modifier.WhenLeaveAny(binder => binder));
        Assert.Same(modifier, modifier.OnUnhandledEvent(_ => Task.CompletedTask));

        IStateMachineModifier<ProbeState> objectState = CreateModifier(new ProbeMachine());
        Assert.Same(objectState, objectState.InstanceState(state => state.CurrentState));
        IStateMachineModifier<ProbeState> textState = CreateModifier(new ProbeMachine());
        Assert.Same(textState, textState.InstanceState(state => state.CurrentStateName));

        var indexedMachine = new ProbeMachine();
        IStateMachineModifier<ProbeState> indexed = CreateModifier(indexedMachine);
        indexed.State("One", out IState<ProbeState> one);
        indexed.State("Two", out IState<ProbeState> two);
        Assert.Same(indexed, indexed.InstanceState(state => state.CurrentStateIndex, one, two));

        var namedMachine = new ProbeMachine();
        IStateMachineModifier<ProbeState> named = CreateModifier(namedMachine);
        named.State("One", out IState<ProbeState> _);
        named.State("Two", out IState<ProbeState> _);
        MethodInfo namedStateMethod = Assert.Single(named.GetType().GetMethods(), method =>
            method.Name == nameof(IStateMachineModifier<ProbeState>.InstanceState)
            && method.GetParameters().Last().ParameterType == typeof(string[]));
        Expression<Func<ProbeState, int>> namedStateExpression = state => state.CurrentStateIndex;
        Assert.Same(named, namedStateMethod.Invoke(named, [namedStateExpression, new[] { "One", "Two" }]));
    }

    static IStateMachineModifier<ProbeState> CreateModifier(ViciOneServiceBusStateMachine<ProbeState> machine)
    {
        Type type = GetInternalGenericType("ViciOne.ServiceBus.Configuration.StateMachineModifier`1");
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        return Assert.IsAssignableFrom<IStateMachineModifier<ProbeState>>(constructor.Invoke([machine]));
    }

    static IStateMachineEventActivitiesBuilder<ProbeState> CreateBuilder(
        ViciOneServiceBusStateMachine<ProbeState> machine,
        IStateMachineModifier<ProbeState> modifier,
        Action<IEventActivities<ProbeState>[]> committer)
    {
        Type type = GetInternalGenericType("ViciOne.ServiceBus.Configuration.StateMachineEventActivitiesBuilder`1");
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        return Assert.IsAssignableFrom<IStateMachineEventActivitiesBuilder<ProbeState>>(constructor.Invoke([machine, modifier, committer]));
    }

    static Type GetInternalGenericType(string name)
    {
        Type? openType = typeof(ViciOneServiceBusStateMachine<>).Assembly.GetType(name);
        Assert.NotNull(openType);
        return openType.MakeGenericType(typeof(ProbeState));
    }

    static void AssertConstructorArgument(string parameterName, Action action)
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        var exception = Assert.IsType<ArgumentNullException>(invocation.InnerException);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static IStateMachineEventActivitiesBuilder<ProbeState> CreateRecordingBuilder(
        IStateMachineModifier<ProbeState> modifier,
        string name,
        ICollection<string> calls,
        Exception? firstFailure = null,
        bool committed = false)
    {
        IStateMachineEventActivitiesBuilder<ProbeState> builder =
            DispatchProxy.Create<IStateMachineEventActivitiesBuilder<ProbeState>, RecordingBuilderProxy>();
        var proxy = (RecordingBuilderProxy)(object)builder;
        proxy.Modifier = modifier;
        proxy.Name = name;
        proxy.Calls = calls;
        proxy.Failure = firstFailure;
        proxy.IsCommitted = committed;
        return builder;
    }

    static void AddTrackedBuilder(
        IStateMachineModifier<ProbeState> modifier,
        IStateMachineEventActivitiesBuilder<ProbeState> builder)
    {
        FieldInfo? field = modifier.GetType().GetField("_activityBuilders", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var builders = Assert.IsAssignableFrom<ICollection<IStateMachineEventActivitiesBuilder<ProbeState>>>(field.GetValue(modifier));
        builders.Add(builder);
    }

    class RecordingBuilderProxy : DispatchProxy
    {
        public required IStateMachineModifier<ProbeState> Modifier { get; set; }
        public required string Name { get; set; }
        public required ICollection<string> Calls { get; set; }
        public Exception? Failure { get; set; }
        public bool IsCommitted { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_IsCommitted")
                return IsCommitted;

            if (targetMethod.Name == nameof(IStateMachineEventActivitiesBuilder<ProbeState>.CommitActivities))
            {
                Calls.Add(Name);
                if (Failure != null)
                {
                    Exception failure = Failure;
                    Failure = null;
                    throw failure;
                }

                IsCommitted = true;
                return Modifier;
            }

            throw new InvalidOperationException($"Unexpected builder call: {targetMethod.Name}");
        }
    }

    class RecordingModifierProxy : DispatchProxy
    {
        public required ICollection<string> Calls { get; set; }
        public required IStateMachineEventActivitiesBuilder<ProbeState> ReturnBuilder { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Calls.Add(targetMethod.Name);

            if (targetMethod.ReturnType == typeof(void))
                return null;

            if (typeof(IStateMachineEventActivitiesBuilder<ProbeState>).IsAssignableFrom(targetMethod.ReturnType))
                return ReturnBuilder;

            if (typeof(IStateMachineModifier<ProbeState>).IsAssignableFrom(targetMethod.ReturnType))
                return this;

            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }

    sealed class ProbeMachine : ViciOneServiceBusStateMachine<ProbeState>
    {
    }

    sealed class ProbeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public CompositeEventStatus CompositeStatus { get; set; }
        public int CompositeBits { get; set; }
        public IState? CurrentState { get; set; }
        public string CurrentStateName { get; set; } = string.Empty;
        public int CurrentStateIndex { get; set; }
    }

    sealed class ProbeMessage
    {
    }

    sealed class MachineMembers
    {
        public IEvent<ProbeMessage> Event { get; set; } = null!;
        public IState State { get; set; } = null!;
        public IState SubState { get; set; } = null!;
    }
}
